// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open SsdpListener
open System
open System.Reactive.Subjects
open System.Threading
open System.Text

type SsdpClient () =

    let mutable disposed = false
    let cts = new CancellationTokenSource()
    let messages = new Subject<string>()
    let rw = new SsdpReaderWriter()
    let rwSub = rw.Packets.Subscribe(fun pkt ->
        let s = Encoding.ASCII.GetString pkt.UdpResult.Buffer
        messages.OnNext s)

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpClient"))

            disposed <- true

            // Clean up managed resources
            cts.Dispose()
            rwSub.Dispose()
            rw.Dispose()
            messages.Dispose()

        // Clean up native resources
        ()

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false

    member __.Messages = messages :> IObservable<_>
