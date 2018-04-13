// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open PerformanceTiming
open SoundMetrics.Aris.Config
open SoundMetrics.Aris.ReorderCS
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
    | Frame of frame: Frame * histogram: FrameHistogram * isRecording: bool
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

    let reorderData (fb : FrameBuffer) =

        let struct (struct (reordered, method), sw) = timeThis (fun _sw ->
            let reorderedSampleData =
                let reorder = TransformFunction(SoundMetrics.Aris.ReorderCS.Reorder.ReorderFrame)
                NativeBuffer.transform
                    reorder
                    fb.PingMode
                    fb.PingsPerFrame
                    fb.BeamCount
                    fb.SampleCount
                    fb.SampleData

            struct (reorderedSampleData, "ReorderCS")
        )

        reordered

    let generateHistogram (fb : FrameBuffer) =

        let struct (histogram, sw) = timeThis (fun _sw ->
            
            let buildHistogram size (source : nativeptr<byte>) =
                FrameHistogram.Generate(source, size)
            
            let size = fb.BeamCount * fb.SampleCount
            fb.SampleData |> NativeBuffer.iter (buildHistogram size)
        )

        histogram

    let inline processFrameBuffer reorderSamples fb =
        let struct (result, sw) = timeThis (fun _sw ->
            let reorderedSamples =
                if reorderSamples then
                    reorderData fb
                else
                    fb.SampleData

            let histogram = generateHistogram fb
            struct (reorderedSamples, histogram)
        )

        result

    type ProcessPipelineState = {
        IsRecording: bool
    }
    with
        static member Create () = { IsRecording = false }

    let processPipeline (performanceReportSink : ConduitPerformanceReportSink)
                        (earlyFrameSpur : ISubject<ProcessedFrame>)
                        (state : ProcessPipelineState ref)
                        (work : WorkUnit) =

        let struct (output, sw) = timeThis (fun sw ->
            match work.work with
            | IncomingFrame frame ->
                let fb = FrameBuffer.FromFrame frame
                let reorderSamples = (frame.Header.ReorderedSamples = 0u)

                let struct (sampleData, histogram) = processFrameBuffer reorderSamples fb
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

        earlyFrameSpur.OnNext (output)

        performanceReportSink.FrameProcessed sw
        output
