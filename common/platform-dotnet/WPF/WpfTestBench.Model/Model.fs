namespace WpfTestBench

open SoundMetrics.Aris.Comms
open System
open System.Threading
open SoundMetrics.Common

[<Sealed>]
type Model (syncContext : SynchronizationContext) =

    let mutable disposed = false

    let beaconListener =
            BeaconListener.CreateForArisExplorerAndVoyager(syncContext, TimeSpan.FromSeconds(30.0))
    let dispose () =    if not disposed then
                            beaconListener.Dispose()
                            disposed <- true


    interface IDisposable with
        member __.Dispose() = dispose()

    member me.Dispose() = (me :> IDisposable).Dispose()

    member __.Beacons = beaconListener.AllBeacons
