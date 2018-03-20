// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open FrameStream
open Serilog
open System
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

[<AutoOpen>]
module private SlidingWindowFrameAssemblerLogging =

    let logSkippedFrame (currentFrame : int) (incomingFrame : int) (incomingDataOffset : int)
                        (gap : uint64) (cause : string) =
        Log.Information("Skipped frame currentFrame={CurrentFrame}; incomingFrame={IncomingFrame}; "
            + "incomingDataOffset={IncomingDataOffset}; gap={Gap}; cause={Cause}",
            currentFrame, incomingFrame, incomingDataOffset, gap, cause)

    let logDuplicatePacket (lastFinishedFrame : int) (incomingFrame : int) (incomingDataOffset : int) =
        Log.Verbose("Duplicate packet lastFinishedFrame={LastFinishedFrame}; incomingFrame={IncomingFrame}; incomingDataOffset={IncomingDataOffset}",
            lastFinishedFrame, incomingFrame, incomingDataOffset)

    let logMissedPacket (currentFrame : int) (incomingFrame : int) (expectedOffset : int) (incomingOffset : int) =
        Log.Verbose("Missed packet currentFrame={CurrentFrame}; incomingFrame={IncomingFrame}; "
            + "expectedOffset={ExpectedOffset}; incomingOffset={IncomingOffset}",
            currentFrame, incomingFrame, expectedOffset, incomingOffset)

    let logFinishedFrame (currentFrame : int) (isComplete : bool) =
        Log.Verbose("Finished frame currentFrame={CurrentFrame}; isComplete={IsComplete}",
            currentFrame, isComplete)

    let logAcceptedPacket (currentFrame : int) (incomingOffset : int) =
        Log.Verbose("Accepted packet currentFrame={CurrentFrame}; incomingOffset={IncomingOffset}",
            currentFrame, incomingOffset)

    let logResetFrameIndexes (lastFinishedFrame : int) =
        Log.Information("Reset last finished frame index to {LastFinishedFrame}", lastFinishedFrame)

    let logReceivedWorkUnit (workUnitType : string) =
        Log.Verbose("Received work unit; type={WorkUnitType}", workUnitType)

    let logReceivingPacket (byteCount : int) =
        Log.Verbose("Receiving packet of {ByteCount} bytes", byteCount)

    let logCouldNotParseOrProcessFramePart (msg : string) =
        Log.Warning("Couldn't parse or process FramePart: {Message}", msg)

    let logIncomingFrameIndexAdvanced (currentFI : int) (incomingFI : int) =
        Log.Verbose("Incoming frame index advanced from {CurrentFI} to {IncomingFI}",
            currentFI, incomingFI)

    let logFirstKnownFrame (fi : int) =
        Log.Verbose("First known frame; fi={FrameIndex}", fi)

    let logSenderMovedOnToNextFrame (fi : int) =
        Log.Verbose("Sender moved on to next frame; fi= {FrameIndex}", fi)

    let logResetCurrentFrameIndexTo (fi : int) =
        Log.Verbose("Reset current frame index; fi={FrameIndex}", fi)

    let logReceivedPacketFromPreviousFrame (fi : int) =
        Log.Verbose("Received packet from previous frame; fi={FrameIndex}", fi)

    let logSendingFramePartAck (fi : int) (offset : int) =
        Log.Verbose("Sending frame part ack; fi={FrameIndex}; offset={Offset}", fi, offset)

    let logFrameComplete (fi : int) (offset : int) =
        Log.Verbose("Frame complete; fi={FrameIndex}; offset={Offset}", fi, offset)

(*
    The sliding window frame assembler implements a simple sliding window method for use
    while assembling frames of packets received.
*)

type internal SendAck = FrameIndex -> int -> unit
type internal FrameFinishedHandler = FrameAccumulator -> unit

[<AutoOpen>]
module private SlidingWindowFrameAssemblerImpl =

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
        expectedDataOffset := 0

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
        assert (incomingDataOffset = 0)

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
            expectedDataOffset := !expectedDataOffset + framePart.Data.Length
            acceptedPacket := true
            logAcceptedPacket incomingFrameIndex incomingDataOffset
        else
            // Missed a part. Could be caused by a missing packet or by ArisApp resetting and
            // starting over.
            logMissedPacket !currentFrameIndex incomingFrameIndex !expectedDataOffset incomingDataOffset

    let onNoFrameInProgress timestamp incomingFrameIndex currentFrameIndex incomingDataOffset expectedDataOffset acceptedPacket
                            currentFrame
                            (framePart: FramePart) =

        if incomingDataOffset = 0 then
            let accum = FrameAccumulator(
                                    incomingFrameIndex, framePart.Header.ToByteArray(), timestamp,
                                    framePart.Data.ToByteArray(), framePart.TotalDataSize)
            currentFrame := Some accum
            expectedDataOffset := framePart.Data.Length
            acceptedPacket := true
            logAcceptedPacket incomingFrameIndex incomingDataOffset
        else
            // Ack will go out asking for the first part of the frame to be resent.
            logMissedPacket !currentFrameIndex incomingFrameIndex 0 incomingDataOffset



/// Assembles packets into frames. See FrameStreamListener for policy regarding
/// packet retries and dropping partial frames.
[<Sealed(true)>]
type SlidingWindowFrameAssembler private (sendAck: SendAck,
                                          onFrameFinished: FrameFinishedHandler) =
    let disposed = ref false
    let currentFrameIndex = ref -1
    let lastFinishedFrameIndex = ref -1
    let expectedDataOffset = ref 0
    let currentFrame: FrameAccumulator option ref = ref None
    let metrics = ref ProtocolMetrics.Empty

    let stateGuard = Object()
    let metricsGuard = Object()

    let updateMetrics update =
        lock metricsGuard (fun () -> metrics := !metrics + update)

    let getMetricsForFinishedFrame (frame: FrameAccumulator) =
        { ProtocolMetrics.Empty with
            uniqueFrameIndexCount = 1UL;
            processedFrameCount = 1UL;
            completeFrameCount = if frame.IsComplete then 1UL else 0UL;
            totalExpectedFrameSize = uint64 frame.ExpectedSize;
            totalReceivedFrameSize = uint64 frame.BytesReceived; }

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
                    let incomingDataOffset = framePart.DataOffset
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

                    if incomingFrameIndex < !currentFrameIndex && incomingDataOffset <> 0 then
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

                        if !expectedDataOffset = framePart.TotalDataSize then
                            logFrameComplete incomingFrameIndex !expectedDataOffset
                            flush false
                    )
            with
                e -> logCouldNotParseOrProcessFramePart e.Message
        finally
            updateMetrics { ProtocolMetrics.Empty with
                                skippedFrameCount = skippedFrameCount.Value
                                totalPacketsReceived = 1UL
                                totalPacketsAccepted = if acceptedPacket.Value then 1UL else 0UL
                                unparsablePackets = if parsedOkay.Value then 0UL else 1UL }


    let processWorkUnit workUnit =
        logReceivedWorkUnit (workUnit.GetType().Name)

        match workUnit with
        | Packet (packetData, timestamp) -> receivePacket packetData timestamp
        | Drain tcs ->  match tcs with
                        | Some t -> t.SetResult(())
                        | None -> ()
        
    let workQueue, pktQueueLink = makeWorkQueue processWorkUnit

    internal new (sendAck, onFrameFinished) =
        new SlidingWindowFrameAssembler(sendAck, onFrameFinished)

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
