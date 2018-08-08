// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open SoundMetrics.Aris.Comms

module FindSonar =

    open System
    open System.Reactive.Linq
    open System.Threading
    open System.Threading.Tasks

    type SonarBeaconListener = Beacons.BeaconSource<SonarBeacon, SerialNumber>

    // Missing from module Observable.
    let private timeout<'T> (timespan : TimeSpan) (obs : IObservable<'T>) =
        Observable.Timeout(obs, timespan)

    /// This is the asynchronous, F#-friendly version of FindArisAsync. If you're using C#, use
    /// the Pascal-cased FindArisAsync.
    /// See BeaconListeners.CreateDefaultSonarBeaconListener.
    [<CompiledName("FindArisAsyncFSharp")>]
    let findArisAsync (availability : SonarBeaconListener) findTimeout sn  = async {

            let mutable beaconFound = None
            use readySignal = new ManualResetEvent(false)

            use _sub = availability.Beacons
                      |> Observable.filter (fun beacon -> beacon.SerialNumber = sn)
                      |> timeout findTimeout
                      |> Observable.subscribe (fun beacon ->
                            beaconFound <- Some beacon
                            readySignal.Set() |> ignore
                         )

            return
                if readySignal.WaitOne(findTimeout : TimeSpan) then
                    beaconFound
                else
                    None
    }

    [<CompiledName("FindArisAsync")>]
    let findArisCSharpAsync (availability : SonarBeaconListener) findTimeout sn : Task<SonarBeacon> =

        Async.StartAsTask(async {
            let! beacon = findArisAsync availability findTimeout sn
            return
                match beacon with
                | Some beacon -> beacon
                | None -> Unchecked.defaultof<SonarBeacon>
        })
