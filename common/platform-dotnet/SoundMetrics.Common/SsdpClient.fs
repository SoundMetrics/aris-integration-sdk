// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

    (*
        Provides a public interface for listening to SSDP messages.
    *)

open SsdpInterfaceInputs
open System
open System.Reactive.Subjects
open System.Threading.Tasks.Dataflow

/// Client interface for listening to SSDP messages.
type SsdpClient () =

    let mutable disposed = false
    let messages = new Subject<_>()
    let listener = new MultiInterfaceListener()

    // Shim from TPL dataflow to Reactive.
    let listenerLink = listener.Messages.LinkTo(ActionBlock<_>(messages.OnNext))

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpClient"))

            disposed <- true

            // Clean up managed resources
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

    member internal __.Messages = messages :> IObservable<_>

    member internal __.MessageSourceBlock = listener.Messages :> ISourceBlock<_>
