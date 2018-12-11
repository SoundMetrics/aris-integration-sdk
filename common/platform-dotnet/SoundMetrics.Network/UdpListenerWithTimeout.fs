// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

open Serilog
open System
open System.Net
open System.Net.Sockets

module internal UdpListenerWithTimeout =

    let private noOpCallback _ = ()

    let listenAsync (timeout : TimeSpan)
                    // callback takes ... -> localEP -> remoteEP -> ...
                    (callback : byte array -> DateTimeOffset -> IPEndPoint -> IPEndPoint -> unit)
                    (udp : UdpClient) =
        async {
            let mutable keepGoing = true
            let deadline = DateTimeOffset.Now + timeout

            while keepGoing do
                let remainingTimeout = deadline - DateTimeOffset.Now
                if remainingTimeout <= TimeSpan.Zero then
                    Log.Debug("listenAsync: timed out")
                    keepGoing <- false
                else
                    let iar = udp.BeginReceive(AsyncCallback noOpCallback, ())
                    let millis = (int remainingTimeout.TotalMilliseconds) + 1
                    let! success = Async.AwaitIAsyncResult(iar, millis)
                    if success then
                        let timestamp = DateTimeOffset.Now
                        let localEP = udp.Client.LocalEndPoint :?> IPEndPoint
                        let remoteEP = IPEndPoint(0L, 0)
                        let bytes = udp.EndReceive(iar, ref remoteEP)
                        callback bytes timestamp localEP remoteEP
        }
