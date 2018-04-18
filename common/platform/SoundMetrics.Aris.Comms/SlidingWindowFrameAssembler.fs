// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open FrameStream
open Serilog
open System
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

[<AutoOpen>]
module private SlidingWindowFrameAssemblerLogging =

    let logVerboseFrameAssembly =
#if LOG_VERBOSE_FRAME_ASSEMBLY
        true
#else
        false
#endif

    let logSkippedFrame (currentFrame : FrameIndex) (incomingFrame : FrameIndex) (incomingDataOffset : uint32)
                        (gap : uint64) (cause : string) =
        if logVerboseFrameAssembly then
            Log.Verbose("Skipped frame currentFrame={CurrentFrame}; incomingFrame={IncomingFrame}; "
                + "incomingDataOffset={IncomingDataOffset}; gap={Gap}; cause={Cause}",
                currentFrame, incomingFrame, incomingDataOffset, gap, cause)

    let logDuplicatePacket (lastFinishedFrame : FrameIndex) (incomingFrame : FrameIndex) (incomingDataOffset : uint32) =
        if logVerboseFrameAssembly then
            Log.Verbose("Duplicate packet lastFinishedFrame={LastFinishedFrame}; incomingFrame={IncomingFrame}; incomingDataOffset={IncomingDataOffset}",
                lastFinishedFrame, incomingFrame, incomingDataOffset)

    let logMissedPacket (currentFrame : FrameIndex) (incomingFrame : FrameIndex) (expectedOffset : uint32) (incomingOffset : uint32) =
        if logVerboseFrameAssembly then
            Log.Verbose("Missed packet currentFrame={CurrentFrame}; incomingFrame={IncomingFrame}; "
                + "expectedOffset={ExpectedOffset}; incomingOffset={IncomingOffset}",
                currentFrame, incomingFrame, expectedOffset, incomingOffset)

    let logFinishedFrame (currentFrame : FrameIndex) (isComplete : bool) =
        if logVerboseFrameAssembly then
            Log.Verbose("Finished frame currentFrame={CurrentFrame}; isComplete={IsComplete}",
                currentFrame, isComplete)

    let logAcceptedPacket (currentFrame : FrameIndex) (incomingOffset : uint32) =
        if logVerboseFrameAssembly then
            Log.Verbose("Accepted packet currentFrame={CurrentFrame}; incomingOffset={IncomingOffset}",
                currentFrame, incomingOffset)

    let logResetFrameIndexes (lastFinishedFrame : FrameIndex) =
        Log.Information("Reset last finished frame index to {LastFinishedFrame}", lastFinishedFrame)

    let logReceivedWorkUnit (workUnitType : string) =
        if logVerboseFrameAssembly then
            Log.Verbose("Received work unit; type={WorkUnitType}", workUnitType)

    let logReceivingPacket (byteCount : int) =
        if logVerboseFrameAssembly then
            Log.Verbose("Receiving packet of {ByteCount} bytes", byteCount)

    let logCouldNotParseOrProcessFramePart (msg : string) stackTrace =
        Log.Warning("Couldn't parse or process FramePart: {Message}; {stackTrace}", msg, stackTrace)

    let logIncomingFrameIndexAdvanced (currentFI : FrameIndex) (incomingFI : FrameIndex) =
        if logVerboseFrameAssembly then
            Log.Verbose("Incoming frame index advanced from {CurrentFI} to {IncomingFI}",
                currentFI, incomingFI)

    let logFirstKnownFrame (fi : FrameIndex) =
        if logVerboseFrameAssembly then
            Log.Verbose("First known frame; fi={FrameIndex}", fi)

    let logSenderMovedOnToNextFrame (fi : FrameIndex) =
        if logVerboseFrameAssembly then
            Log.Verbose("Sender moved on to next frame; fi= {FrameIndex}", fi)

    let logResetCurrentFrameIndexTo (fi : FrameIndex) =
        if logVerboseFrameAssembly then
            Log.Verbose("Reset current frame index; fi={FrameIndex}", fi)

    let logReceivedPacketFromPreviousFrame (fi : FrameIndex) =
        if logVerboseFrameAssembly then
            Log.Verbose("Received packet from previous frame; fi={FrameIndex}", fi)

    let logSendingFramePartAck (fi : FrameIndex) (offset : uint32) =
        if logVerboseFrameAssembly then
            Log.Verbose("Sending frame part ack; fi={FrameIndex}; offset={Offset}", fi, offset)

    let logFrameComplete (fi : FrameIndex) (offset : uint32) =
        if logVerboseFrameAssembly then
            Log.Verbose("Frame complete; fi={FrameIndex}; offset={Offset}", fi, offset)

(*
    The sliding window frame assembler implements a simple sliding window method for use
    while assembling frames of packets received.
*)

type internal SendAck = FrameIndex -> uint32 -> unit
type internal FrameFinishedHandler = FrameAccumulator -> unit

module private SlidingWindowFrameAssemblerDetails =

    type WorkUnit =
    | Packet of data : byte array * timestamp : DateTimeOffset
    | Drain of TaskCompletionSource<unit> option

    /// Construct the TPL data graph that makes up the packet queue, which
    /// buffers then invokes processPacket. Returns the graph entry point
    /// and the disposable to tear down the graph.
    let makeWorkQueue (processWorkUnit: WorkUnit -> unit) =
        let pktQueue = BufferBlock<WorkUnit>()
        let pktProcessor = ActionBlock<WorkUnit>(processWorkUnit)
        let pktQueueLink = pktQueue.LinkTo(pktProcessor)
        // Return the entry into the graph and the disposable.
        pktQueue, pktQueueLink

    let onFrameIndexAdvanced currentFrameIndex incomingFrameIndex incomingDataOffset expectedDataOffset setSkipCount
                             flush =

        logIncomingFrameIndexAdvanced !currentFrameIndex incomingFrameIndex

        if !currentFrameIndex < 0 then
            // This is the first frame we're starting
            logFirstKnownFrame incomingFrameIndex
        else
            // sender moved on to the next frame
            logSenderMovedOnToNextFrame incomingFrameIndex

            let gap, _ = setSkipCount()
            logSkippedFrame !currentFrameIndex incomingFrameIndex incomingDataOffset gap
                            "sender moved on to the next frame"
            flush false

        System.Diagnostics.Trace.TraceInformation(sprintf "Reset currentFrameIndex to %d" !currentFrameIndex)
        logResetCurrentFrameIndexTo incomingFrameIndex
        currentFrameIndex := incomingFrameIndex
        expectedDataOffset := 0u

    let onReceivedPacketFromPreviousFrame incomingFrameIndex
                                          currentFrameIndex
                                          incomingDataOffset
                                          setSkipCount =

        // duplicate packet from finished frame
        logReceivedPacketFromPreviousFrame incomingFrameIndex

        let gap, _ = setSkipCount()
        logSkippedFrame !currentFrameIndex incomingFrameIndex incomingDataOffset gap
                        "duplicate packet from finished frame"

    let onRetrogradeFrameIndex incomingFrameIndex currentFrameIndex lastFinishedFrameIndex incomingDataOffset
                               flush =

        // Sonar may have reset; resync on first frame of a packet; we just checked
        // for incomingDataOffset <> 0 a few lines above.
        assert (incomingDataOffset = 0u)

        // Flush first (side effect), then reset indexes.
        flush true
        let newIndex = incomingFrameIndex
        currentFrameIndex := newIndex
        logResetFrameIndexes !lastFinishedFrameIndex

    let onFrameInProgress incomingFrameIndex currentFrameIndex incomingDataOffset expectedDataOffset acceptedPacket
                          (framePart: FramePart)
                          (accum: FrameAccumulator) =

        if incomingDataOffset = !expectedDataOffset then
            accum.AppendFrameData incomingDataOffset (framePart.Data.ToByteArray())
            expectedDataOffset := !expectedDataOffset + uint32 framePart.Data.Length
            acceptedPacket := true
            logAcceptedPacket incomingFrameIndex incomingDataOffset
        else
            // Missed a part. Could be caused by a missing packet or by ArisApp resetting and
            // starting over.
            logMissedPacket !currentFrameIndex incomingFrameIndex !expectedDataOffset incomingDataOffset

    let onNoFrameInProgress timestamp incomingFrameIndex currentFrameIndex incomingDataOffset expectedDataOffset acceptedPacket
                            currentFrame
                            (framePart: FramePart) =

        if incomingDataOffset = 0u then
            let accum = FrameAccumulator(
                                    incomingFrameIndex, framePart.Header.ToByteArray(), timestamp,
                                    framePart.Data.ToByteArray(), uint32 framePart.TotalDataSize)
            currentFrame := Some accum
            expectedDataOffset := uint32 framePart.Data.Length
            acceptedPacket := true
            logAcceptedPacket incomingFrameIndex incomingDataOffset
        else
            // Ack will go out asking for the first part of the frame to be resent.
            logMissedPacket !currentFrameIndex incomingFrameIndex 0u incomingDataOffset

open SlidingWindowFrameAssemblerDetails

/// Assembles packets into frames. See FrameStreamListener for policy regarding
/// packet retries and dropping partial frames.
[<Sealed(true)>]
type internal SlidingWindowFrameAssembler (sendAck: SendAck,
                                           onFrameFinished: FrameFinishedHandler) =
    let disposed = ref false
    let currentFrameIndex = ref -1
    let lastFinishedFrameIndex = ref -1
    let expectedDataOffset = ref 0u
    let currentFrame: FrameAccumulator option ref = ref None
    let metrics = ref ProtocolMetrics.Empty

    let stateGuard = Object()
    let metricsGuard = Object()

    let updateMetrics update =
        lock metricsGuard (fun () -> metrics := !metrics + update)

    let getMetricsForFinishedFrame (frame: FrameAccumulator) =
        { ProtocolMetrics.Empty with
            UniqueFrameIndexCount = 1UL;
            ProcessedFrameCount = 1UL;
            CompleteFrameCount = if frame.IsComplete then 1UL else 0UL;
            TotalExpectedFrameSize = uint64 frame.ExpectedSize;
            TotalReceivedFrameSize = uint64 frame.BytesReceived; }

    /// Assigns a new frame index to the frame (mutation!)
    let assignNewFrameIndex = (* FrameAccumulator -> unit *)
        // Close over state monotonicFrameIndex.
        let monotonicFrameIndex = ref 0 // Frame numbers start at zero. Often this is overwritten
                                        // (e.g. during recording) but we start at zero.
        fun (frame: FrameAccumulator) ->
            let idx = !monotonicFrameIndex
            frame.SetFrameIndex idx
            monotonicFrameIndex := idx + 1

    let flush clearFrameIndex =
        lock  stateGuard (fun () -> 
            match !currentFrame with
            | Some frame ->
                currentFrame := None
                assignNewFrameIndex frame
                updateMetrics (getMetricsForFinishedFrame frame)
                lastFinishedFrameIndex := frame.FrameIndex
                logFinishedFrame frame.FrameIndex frame.IsComplete
                currentFrameIndex := !currentFrameIndex + 1
                onFrameFinished frame
            | None -> ()

            if clearFrameIndex then
                lastFinishedFrameIndex := -1
        )

    // Process a packet received on the frame stream.
    let receivePacket (data: byte array) (timestamp : DateTimeOffset) =
        logReceivingPacket data.Length

        // Maintain partial metrics for updating metrics below.
        let acceptedPacket = ref false
        let skippedFrameCount = ref 0UL
        let parsedOkay = ref false

        try // finally
            try // with
                let framePart = FramePart.Parser.ParseFrom(data)
                parsedOkay := true

                lock stateGuard (fun () ->
                    let incomingFrameIndex = framePart.FrameIndex
                    let incomingDataOffset = uint32 framePart.DataOffset

                    let setSkipCount () =
                        let gap = uint64(incomingFrameIndex - !currentFrameIndex - 1)
                        let newValue = !skippedFrameCount + gap
                        skippedFrameCount := newValue
                        gap, newValue

                    // The sonar is responsible for deciding  when to move on to the next frame
                    // so regardless of our state we move on with it.
                    if incomingFrameIndex > !currentFrameIndex then
                        onFrameIndexAdvanced currentFrameIndex incomingFrameIndex incomingDataOffset expectedDataOffset
                                             setSkipCount flush

                    if incomingFrameIndex < !currentFrameIndex && incomingDataOffset <> 0u then
                        onReceivedPacketFromPreviousFrame incomingFrameIndex currentFrameIndex incomingDataOffset
                                                          setSkipCount
                    else
                        if incomingFrameIndex < !currentFrameIndex then
                            onRetrogradeFrameIndex incomingFrameIndex currentFrameIndex lastFinishedFrameIndex incomingDataOffset
                                                   flush

                        match !currentFrame with
                        | Some cf ->
                            onFrameInProgress incomingFrameIndex currentFrameIndex incomingDataOffset expectedDataOffset
                                              acceptedPacket framePart cf

                        | None ->
                            onNoFrameInProgress timestamp incomingFrameIndex currentFrameIndex incomingDataOffset expectedDataOffset
                                                acceptedPacket currentFrame framePart

                        // NOTE: we're always acking each packet for now; this should change when we
                        // develop strategies for retrying packets.
                        logSendingFramePartAck incomingFrameIndex !expectedDataOffset
                        sendAck incomingFrameIndex !expectedDataOffset

                        if !expectedDataOffset = uint32 framePart.TotalDataSize then
                            logFrameComplete incomingFrameIndex !expectedDataOffset
                            flush false
                    )
            with
                e -> logCouldNotParseOrProcessFramePart e.Message e.StackTrace
        finally
            updateMetrics { ProtocolMetrics.Empty with
                                SkippedFrameCount = skippedFrameCount.Value
                                TotalPacketsReceived = 1UL
                                TotalPacketsAccepted = if acceptedPacket.Value then 1UL else 0UL
                                UnparsablePackets = if parsedOkay.Value then 0UL else 1UL }


    let processWorkUnit workUnit =
        logReceivedWorkUnit (workUnit.GetType().Name)

        match workUnit with
        | Packet (packetData, timestamp) -> receivePacket packetData timestamp
        | Drain tcs ->  match tcs with
                        | Some t -> t.SetResult(())
                        | None -> ()
        
    let workQueue, pktQueueLink = makeWorkQueue processWorkUnit

    interface IDisposable with
        member __.Dispose() =
            Dispose.theseWith disposed
                [ pktQueueLink ]
                (fun () ->
                    workQueue.Complete()
                    workQueue.Completion.Wait())
    member s.Dispose() = (s :> IDisposable).Dispose()

    /// Flushes the current frame and resets to expect any frame index next.
    member __.Flush () = flush true
    member __.Metrics = !metrics
    member __.ProcessPacket packetData = workQueue.Post(Packet packetData)
    member __.DrainAsync () =
        let tcs = TaskCompletionSource<unit>()
        workQueue.Post(Drain (Some tcs)) |> ignore
        tcs.Task
