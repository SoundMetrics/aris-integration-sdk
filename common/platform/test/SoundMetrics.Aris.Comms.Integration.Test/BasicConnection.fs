module BasicConnection

open Serilog
open SoundMetrics.Aris.Comms
open System
open TestInputs

let testBasicConnection (inputs : TestInputs) =

    // Console doesn't have a sync context by default, we need one for the beacon listener.
    let syncContext = Threading.SynchronizationContext()
    use availability =
            BeaconListeners.mkSonarBeaconListener
                NetworkConstants.SonarAvailabilityListenerPortV2
                (TimeSpan.FromSeconds(30.0))
                syncContext
                Beacons.BeaconExpirationPolicy.KeepExpiredBeacons
                None // callbacks

    use sub = availability.Beacons
                |> Observable.filter (fun beacon -> beacon.serialNumber = sn)
                |> Observable.timeout timeoutPeriod
                |> Observable.subscribe (fun beacon ->
                    targetIpAddr <- beacon.srcIpAddr
                    softwareVersion <- beacon.softwareVersion
                    readySignal.Set() |> ignore
                    )
    if readySignal.WaitOne(timeoutPeriod : TimeSpan) then
        Some (targetIpAddr, softwareVersion)
    else
        None
