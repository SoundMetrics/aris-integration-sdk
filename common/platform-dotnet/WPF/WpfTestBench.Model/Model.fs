namespace WpfTestBench

open SoundMetrics.Aris.Comms
open System
open System.Threading

[<Sealed>]
type Model (syncContext : SynchronizationContext) =

    let mutable disposed = false

    let beaconListener =
            BeaconListeners.createSonarBeaconListener
                (TimeSpan.FromSeconds(30.0))
                syncContext
                Beacons.BeaconExpirationPolicy.KeepExpiredBeacons
                None // callbacks

    let dispose () =    if not disposed then
                            beaconListener.Dispose()
                            disposed <- true


    interface IDisposable with
        member __.Dispose() = dispose()

    member me.Dispose() = (me :> IDisposable).Dispose()

    member __.Beacons = beaconListener.Beacons
