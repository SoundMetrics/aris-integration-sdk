// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System
open System.Net
open System.Net.Sockets
open System.Reactive.Subjects
open System.Threading

module Udp =

    type UdpReceived = { udpResult: UdpReceiveResult; timestamp: DateTime }

    /// Returns the tuple (ISubject<UdpReceived>, IDisposable).
    let internal makeUdpListener (addr : IPAddress) port (reuseAddr: bool) =
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
                let keepGoing = t.IsCompleted && not t.IsFaulted && not cts.IsCancellationRequested
                if keepGoing then
                    let now = DateTime.Now
                    packetSubject.OnNext { udpResult = task.Result; timestamp = now }
                    listen()
                else
                    doneSignal.Set() )
            task.ContinueWith(action) |> ignore

        udp.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuseAddr);
        udp.Client.Bind (new IPEndPoint(addr, port));
        listen()

        let disposable = Dispose.makeDisposable
                            (fun () -> 
                                // Stop listening to the socket
                                cts.Cancel ()
                                udp.Close () // Because UdpClient doesn't know about cancellation--may make it throw.
                                doneSignal.Wait ()

                                // Clean up
                                packetSubject.OnCompleted () )
                            [udp; packetSubject; cts; doneSignal]

        packetSubject :> ISubject<UdpReceived>, disposable
