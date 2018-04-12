// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.NativeInterop
open PerformanceTiming
open SoundMetrics.Aris.Config
open SoundMetrics.NativeMemory
open System
open System.Diagnostics
open System.Reactive.Subjects

// For native pointers:
// warning FS0009: Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn "9"'.
#nowarn "9"

type WorkType =
    | IncomingFrame of Frame
    | Command of ProcessingCommand
    | Quit

type WorkUnit = {
    enqueueTime: Stopwatch
    work: WorkType
}
with
    static member Frame frame = { work = IncomingFrame frame; enqueueTime = Stopwatch.StartNew () }
    static member Command cmd = { work = Command cmd; enqueueTime = Stopwatch.StartNew () }
    static member Quit = { work = WorkType.Quit; enqueueTime = Stopwatch.StartNew () }

type ProcessedFrameType =
    | Frame of frame: Frame * histogram: Histogram * isRecording: bool
    | Command of ProcessingCommand
    | Quit

type ProcessedFrame = {
    enqueueTime: Stopwatch
    work: ProcessedFrameType
}
with
    static member Frame (workUnit: WorkUnit) frame histogram isRecording =
        { work = Frame (frame, histogram, isRecording); enqueueTime = workUnit.enqueueTime }

    static member Command (workUnit: WorkUnit) (cmd: ProcessingCommand) = { work = Command cmd; enqueueTime = workUnit.enqueueTime }
    static member Quit (workUnit: WorkUnit) = { work = ProcessedFrameType.Quit; enqueueTime = workUnit.enqueueTime }

module internal FrameProcessing =
    open Serilog

    let private logTimeToProcessFrame (stopwatch : Stopwatch) =
        let duration = PerformanceTiming.formatTiming stopwatch
        Log.Verbose("Time to process frame: {duration}", duration)

    type FrameBuffer = {
        FrameIndex: FrameIndex
        PingMode : uint32
        BeamCount : uint32
        SampleCount: uint32
        PingsPerFrame: uint32
        SampleData: NativeBuffer
    }
    with
        static member FromFrame (f: Frame) =
            let cfg = SonarConfig.getPingModeConfig (PingMode.From (uint32 f.Header.PingMode))
            {
                FrameIndex = int f.Header.FrameIndex
                PingMode = f.Header.PingMode
                BeamCount  = cfg.ChannelCount
                SampleCount = f.Header.SamplesPerBeam
                PingsPerFrame = cfg.PingsPerFrame
                SampleData = f.SampleData
            }

    let channelReverseMap = [| 10; 2; 14; 6; 8; 0; 12; 4;
                               11; 3; 15; 7; 9; 1; 13; 5 |]

    let buildChannelReverseMultiples beamsPerPing pingsPerFrame =
        channelReverseMap |> Seq.take beamsPerPing
                          |> Seq.mapi (fun n _value -> channelReverseMap.[n] * pingsPerFrame)
                          |> Seq.toArray

    /// Used in place of 'for x in 1 .. 4 .. 16 do' as the 'skip' flavor uses a slow enumerator;
    /// syntax: forSkip 1 4 16 (fun x -> ...)
    let inline forSkip start skip finish (f: ^t -> unit) =
        let mutable n: ^t = start
        while n <= finish do
            f n
            n <- n + skip

    let inline reorderSampleBuffer (histogram : Histogram)
                                   (primitivePingMode : uint32,
                                    pingsPerFrame : uint32,
                                    beamCount : uint32,
                                    samplesPerBeam : uint32,
                                    src : nativeint,
                                    dest : nativeint) =

        let outbuf = NativePtr.ofNativeInt<byte> dest
        let inputW = NativePtr.ofNativeInt<uint32> src

        let beamsPerPing = beamCount / pingsPerFrame
        let channelReverseMultipledMap = buildChannelReverseMultiples (int beamsPerPing) (int pingsPerFrame)

        let sampleStride = beamCount
        let bytesReadPerPing = sampleStride / pingsPerFrame

        let updateHisto, disposeHistoUpdater = histogram.CreateUpdater ()

        let sizeofUint = 4u
        let dwordsToReadPerPing = bytesReadPerPing / sizeofUint
        let totalDwordsPerPing = (beamCount * samplesPerBeam / pingsPerFrame) / sizeofUint
        assert(totalDwordsPerPing = (totalDwordsPerPing / dwordsToReadPerPing) * dwordsToReadPerPing)
        let bytesWritten = ref 0u // ref cell because of lambda expression used with forSkip can't capture mutables
        let inputOffset = ref 0u // outside the loops for perfiness

        // Note that "for i = x to y do" does not allow for an unsigned range

        let mutable idxDwordPing0 = 0u
        for idxSample = 0 to int samplesPerBeam - 1 do
            let mutable idxLocalSample = idxDwordPing0
            for idxPing = 0 to int pingsPerFrame - 1 do
                //for idxDword in idxLocalSample .. 4 .. idxLocalSample + dwordsToReadPerPing - 1 do // slow due to enumerator used
                forSkip idxLocalSample 4u (idxLocalSample + dwordsToReadPerPing - 1u) (fun idxDword ->
                    let composed = idxSample * int beamCount + idxPing

                    assert((dwordsToReadPerPing / 4u) * 4u = dwordsToReadPerPing)
                    inputOffset := 0u // ref cell because of lambda expression used with forSkip can't capture mutables
                    //for channel in 0 .. 4 .. beamsPerPing - 1 do // slow due to enumerator used
                    forSkip 0 4 (int beamsPerPing - 1) (fun channel ->
                        //let p = NativePtr.add inputW (int (idxDword + !inputOffset)) // inputW[idxDword + inputOffset]
                        //let values1 = NativePtr.read<uint32> p
                        let values1 = NativePtr.get inputW (int (idxDword + !inputOffset))

                        let outIndex1 = channelReverseMultipledMap.[channel] + composed
                        let outIndex2 = channelReverseMultipledMap.[channel+1] + composed
                        let outIndex3 = channelReverseMultipledMap.[channel+2] + composed
                        let outIndex4 = channelReverseMultipledMap.[channel+3] + composed

                        let byte1 = byte (values1 &&& uint32 0xFF)
                        let byte2 = byte ((values1 >>> 8) &&& uint32 0xFF)
                        let byte3 = byte ((values1 >>> 16) &&& uint32 0xFF)
                        let byte4 = byte ((values1 >>> 24) &&& uint32 0xFF)
                        NativePtr.write<byte> (NativePtr.add outbuf outIndex1) byte1
                        NativePtr.write<byte> (NativePtr.add outbuf outIndex2) byte2
                        NativePtr.write<byte> (NativePtr.add outbuf outIndex3) byte3
                        NativePtr.write<byte> (NativePtr.add outbuf outIndex4) byte4

//                        histogram.Increment (int byte1)
//                        histogram.Increment (int byte2)
//                        histogram.Increment (int byte3)
//                        histogram.Increment (int byte4)
                        updateHisto (int byte1)
                        updateHisto (int byte2)
                        updateHisto (int byte3)
                        updateHisto (int byte4)

                        bytesWritten := !bytesWritten + 4u
                        inputOffset := !inputOffset + 1u
                        )
                    )

                idxLocalSample <- idxLocalSample + totalDwordsPerPing
                (* end: for idxDword in idxLocalSample .. 4 .. idxLocalSample + dwordsToReadPerPing - 1 do *)
            idxDwordPing0 <- idxDwordPing0 + dwordsToReadPerPing
            (* end: for idxPing in 0 .. pingsPerFrame - 1 do *)

        disposeHistoUpdater ()

    let generateHistogram (fb : FrameBuffer) =
        let histogram = Histogram.Create ()
        let updateHisto, disposeHistoUpdater = histogram.CreateUpdater ()

        let buildHistogram (source : nativeptr<byte>) =
            let bytes = source
            for offset = 0 to int fb.SampleData.Length - 1 do
                let sample = NativePtr.get bytes offset
                updateHisto (int sample)

        try
            fb.SampleData |> NativeBuffer.iter buildHistogram
            histogram
        finally
            disposeHistoUpdater()

    let reorderData (fb : FrameBuffer) =

        let struct ((result, method), sw) = timeThis (fun () ->
            let histogram = Histogram.Create ()

            let reorderedSampleData =
                let reorder = TransformFunction(SoundMetrics.Aris.ReorderCS.Reorder.ReorderFrame)
                NativeBuffer.transform
                    reorder
                    fb.PingMode
                    fb.PingsPerFrame
                    fb.BeamCount
                    fb.SampleCount
                    fb.SampleData
            (reorderedSampleData, histogram), "ReorderCS"
        )

        Log.Verbose("{method} {duration}", method, PerformanceTiming.formatTiming sw)
        result

    let inline processFrameBuffer reorderSamples fb =
        if reorderSamples then
            reorderData fb
        else
            fb.SampleData, (generateHistogram fb)

    type ProcessPipelineState = {
        IsRecording: bool
    }
    with
        static member Create () = { IsRecording = false }

    let processPipeline (earlyFrameSpur: ISubject<ProcessedFrame>)
                        (state: ProcessPipelineState ref)
                        (work: WorkUnit) =

        let struct (output, sw) = timeThis (fun () ->
            match work.work with
            | IncomingFrame frame ->
                let fb = FrameBuffer.FromFrame frame
                let reorderSamples = (frame.Header.ReorderedSamples = 0u)
                let sampleData, histogram = processFrameBuffer reorderSamples fb
                let newF =
                    if reorderSamples then
                        let mutable hdr = frame.Header
                        hdr.ReorderedSamples <- 1u
                        { Header = hdr; SampleData = sampleData }
                    else
                        frame

                let usingOriginalSampleData = Object.ReferenceEquals(frame.SampleData, sampleData)
                if not usingOriginalSampleData then
                    frame.SampleData.Dispose()

                ProcessedFrame.Frame work newF histogram (!state).IsRecording

            | WorkType.Command cmd ->
                state := {
                    IsRecording = match cmd with
                                    | StartRecording _ -> true
                                    | StopRecording _ -> false
                                    | StopStartRecording _ -> true
                }

                ProcessedFrame.Command work cmd

            | WorkType.Quit -> ProcessedFrame.Quit work
        )

        logTimeToProcessFrame sw
        earlyFrameSpur.OnNext (output)
        output
