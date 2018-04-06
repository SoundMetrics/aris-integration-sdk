// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

module FindSonar =

    open SoundMetrics.Aris.Config
    open System
    open System.Net
    open System.Reactive
    open System.Reactive.Linq
    open System.Runtime.InteropServices
    open System.Threading

    type SonarBeaconListener = Beacons.BeaconSource<SonarBeacon, SerialNumber>

    // Missing from module Observable.
    let private timeout<'T> (timespan : TimeSpan) (obs : IObservable<'T>) =
        Observable.Timeout(obs, timespan)

    let findAris (availability : SonarBeaconListener) timeoutPeriod sn  =

        let mutable beaconFound = None
        use readySignal = new ManualResetEvent(false)

        use sub = availability.Beacons
                  |> Observable.filter (fun beacon -> beacon.SerialNumber = sn)
                  |> timeout timeoutPeriod
                  |> Observable.subscribe (fun beacon ->
                        beaconFound <- Some beacon
                        readySignal.Set() |> ignore
                     )
        if readySignal.WaitOne(timeoutPeriod : TimeSpan) then
            beaconFound
        else
            None

    [<CompiledName("FindAris")>]
    let findArisFiendly(availability : SonarBeaconListener,
                        timeoutPeriod : TimeSpan,
                        sn : SerialNumber,
                        [<Out>] beaconFound : SonarBeacon byref) : bool =

        match findAris availability timeoutPeriod sn with
        | Some beacon ->    beaconFound <- beacon
                            true
        | None ->           beaconFound <- Unchecked.defaultof<_> // null for interop
                            false
