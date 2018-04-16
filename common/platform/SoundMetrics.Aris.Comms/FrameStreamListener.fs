// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open FrameStream
open Google.Protobuf
open System
open System.Diagnostics
open System.Net
open System.Net.Sockets
open System.Reactive.Subjects
open System.Threading

[<AutoOpen>]
module private FrameStreamListenerImpl =
    let makeUdpClient (sinkAddress : IPAddress) =
        let udpClient = new UdpClient(IPEndPoint(sinkAddress, 0)) // any arbitrary port
        assert udpClient.Client.IsBound
        udpClient.Client.ReceiveBufferSize <- 2 * 1024 * 1024
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        let localEndPointPort = (udpClient.Client.LocalEndPoint :?> IPEndPoint).Port
        Debug.WriteLine(sprintf "FrameStreamListener: local listen port is %d" localEndPointPort)
        udpClient

type FrameStreamReliabilityPolicy =
| DropPartialFrames

/// Listens for frames. If the FrameStreamReliabilityPolicy calls for dropping partial frames that
/// dropping is done by this type.
type FrameStreamListener (sinkAddress : IPAddress, frameStreamReliabilityPolicy: FrameStreamReliabilityPolicy) =

    let remoteEndPoint = ref (IPEndPoint(0L, 0))
    let disposed = ref false
    let disposing = ref false // for race conditions
    let nextFrameIndex = ref 0

    // UdpClient is itself a reference type, so it can be updated atomically.
    let mutable udpClient = makeUdpClient sinkAddress

    let udpSender = new UdpClient(0) // any port
    let cts = new CancellationTokenSource()
    let frameSubject = new Subject<Frame>()
    let completeSignal = new ManualResetEventSlim()

    let sendAck (frameIndex : FrameIndex) (dataOffset : uint32) =
        let ack = FramePartAck(FrameIndex = frameIndex,
                               DataOffset = int dataOffset)
        let buf =
            let b = Array.zeroCreate<byte> (ack.CalculateSize())
            use stream = new CodedOutputStream(b)
            ack.WriteTo(stream)
            stream.Flush()
            b
        udpSender.Send(buf, buf.Length, !remoteEndPoint) |> ignore

    let onNewFrame (accum: FrameAccumulator) =
        
        if not !disposing then
            let keepFrame =
                match frameStreamReliabilityPolicy with
                | FrameStreamReliabilityPolicy.DropPartialFrames ->
                    let frameIsComplete = accum.IsComplete
                    if not frameIsComplete then /// REVIEW compile list of modules that should use event sources instead of Trace
                        Trace.TraceInformation("FrameStreamListener: dropping partial frame")
                    frameIsComplete

            if keepFrame then
                let frameIndex = !nextFrameIndex
                if frameSubject.HasObservers then
                    let frame = {
                        Header = Frame.HeaderFrom(accum.HeaderBytes,
                                                  accum.FrameReceiptTimestamp,
                                                  (Some (uint32 frameIndex)))
                        SampleData = accum.SampleData
                    }
                    frameSubject.OnNext frame

                nextFrameIndex := frameIndex + 1

    let frameAssembler = new SlidingWindowFrameAssembler(sendAck, onNewFrame)

    let updateSinkAddress sinkAddress =

        if sinkAddress <> (udpClient.Client.LocalEndPoint :?> IPEndPoint).Address then
            let oldClient = udpClient
            let newClient = makeUdpClient sinkAddress
            udpClient <- newClient
            oldClient.Close()

        udpClient.Client.LocalEndPoint :?> IPEndPoint

    let listen = async {
        let! ct = Async.CancellationToken
        try
            while not ct.IsCancellationRequested do
                // Avoid try/catch in the inner loop, it turns out to be a
                // performance hit. So put it in an outer loop.
                try
                    while not ct.IsCancellationRequested do
                        // Not using ReceiveAsync here definitely helps performance.
                        let udpReceiveResult = udpClient.Receive(remoteEndPoint)
                        let timestamp = DateTimeOffset.Now

                        // On Linux/raspi we sometimes receive spurious empty packets on
                        // 0.0.0.0:0, which is the default value before the sonar's IP address
                        // is known. Ignore these.
                        if udpReceiveResult.Length > 0 then
                            frameAssembler.ProcessPacket (udpReceiveResult, timestamp) |> ignore
                with
                    _ -> () // Try again.
        finally
            completeSignal.Set()
    }

    do
        Async.Start(listen, cts.Token)

    interface IDisposable with
        member __.Dispose() =
            Dispose.theseWith disposed [frameAssembler; cts; completeSignal; frameSubject]
                (fun () ->
                    disposing := true
                    frameSubject.OnCompleted()
                    cts.Cancel()
                    udpClient.Close() // Close before wait so it will complete.
                    completeSignal.Wait())
    member s.Dispose() = (s :> IDisposable).Dispose()

    /// Flushes the current frame and resets to expect any frame index next.
    member __.Flush () = frameAssembler.Flush()
    member __.SetSinkAddress sinkAddress = updateSinkAddress sinkAddress
    member __.SinkEndpoint = udpClient.Client.LocalEndPoint :?> IPEndPoint
    member __.Frames = frameSubject :> IObservable<Frame>
    member __.Metrics = frameAssembler.Metrics
