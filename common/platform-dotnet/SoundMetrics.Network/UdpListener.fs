// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

    (*
        This file provides generic support for listening to a socket for UDP packets.
    *)

open System
open System.Net
open System.Net.Sockets
open System.Reactive.Subjects
open System.Threading

type UdpReceived = { UdpResult: UdpReceiveResult; Timestamp: DateTime }

type UdpListener (addr : IPAddress, port, reuseAddr : bool) =

    // Shutdown
    let cts = new CancellationTokenSource ()
    let doneSignal = new ManualResetEventSlim ()

    // Get & publish packets
    let packetSubject = new Subject<UdpReceived> ()
    let udp = new UdpClient ()

    let rec listen () =

        // Switched to task-based rather than wrapping in Async as sometimes the Async
        // just never completes. And that prevents the process from terminating.

        let task = udp.ReceiveAsync()
        let action = Action<System.Threading.Tasks.Task<UdpReceiveResult>>(fun t ->
            let now = DateTime.Now
            let keepGoing = t.IsCompleted && not t.IsFaulted && not cts.IsCancellationRequested
            if keepGoing then
                packetSubject.OnNext { UdpResult = task.Result; Timestamp = now }
                listen()
            else
                doneSignal.Set() )
        task.ContinueWith(action) |> ignore

    do
        udp.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuseAddr);
        udp.Client.Bind (new IPEndPoint(addr, port));
        listen()

    interface IDisposable with

        override __.Dispose() =
            // Stop listening to the socket
            cts.Cancel ()
            udp.Close () // Because UdpClient doesn't know about cancellation--may make it throw.
            doneSignal.Wait ()

            // Clean up
            packetSubject.OnCompleted ()

            let otherDisposables : IDisposable list = [udp; packetSubject; cts; doneSignal]
            otherDisposables |> List.iter (fun d -> if d <> null then d.Dispose())

    member ul.Dispose() = (ul :> IDisposable).Dispose()

    member __.Packets : IObservable<UdpReceived> = packetSubject :> IObservable<_>
