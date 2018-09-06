// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open System
open System.Threading

type SsdpService (periodicAnnouncement : TimeSpan option) =

    let mutable disposed = false
    let cts = new CancellationTokenSource()

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpService"))

            disposed <- true

            // Clean up managed resources
            cts.Dispose()

        // Clean up native resources
        ()

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false
