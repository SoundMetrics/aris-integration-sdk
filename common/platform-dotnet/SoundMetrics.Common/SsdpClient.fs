// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open SsdpInterfaceInputs
open System
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks.Dataflow

type SsdpClient () =

    let mutable disposed = false
    let cts = new CancellationTokenSource()
    let messages = new Subject<_>()
    let listener = new MultiInterfaceListener()

    let listenerLink = listener.Packets.LinkTo(ActionBlock<_>(messages.OnNext))

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpClient"))

            disposed <- true

            // Clean up managed resources
            cts.Dispose()
            listenerLink.Dispose()
            listener.Dispose()
            messages.Dispose()

        // Clean up native resources
        ()

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false

    member __.Messages = messages :> IObservable<_>
