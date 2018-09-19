// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

open System
open System.Net
open System.Net.Sockets

module internal UdpListenerWithTimeout =

    let private noOpCallback _ = ()

    let listenAsync (timeout : TimeSpan)
                    (callback : byte array -> DateTimeOffset -> IPEndPoint -> unit)
                    (udp : UdpClient) =
        async {
            let mutable keepGoing = true
            let deadline = DateTimeOffset.Now + timeout

            while keepGoing do
                let remainingTimeout = deadline - DateTimeOffset.Now
                if remainingTimeout <= TimeSpan.Zero then
                    keepGoing <- false
                else
                    let iar = udp.BeginReceive(AsyncCallback noOpCallback, ())
                    let millis = (int remainingTimeout.TotalMilliseconds) + 1
                    let! success = Async.AwaitIAsyncResult(iar, millis)
                    if success then
                        let timestamp = DateTimeOffset.Now
                        let ep = IPEndPoint(0L, 0)
                        let bytes = udp.EndReceive(iar, ref ep)
                        callback bytes timestamp ep
        }
