// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

/// Abstractions about listening for beacons.
module Beacons =
    open Serilog
    open System
    open System.Collections.Generic
    open System.Collections.ObjectModel
    open System.Reactive.Linq
    open System.Reactive.Concurrency
    open System.Reactive.Subjects
    open System.Threading
    open System.Threading.Tasks


    /// Mutable wrapper that allows us to update the status without causing notification
    /// of the observable collection changing.
    type StatusHolder<'B>(beacon : 'B, timestamp : DateTimeOffset) as self =
        inherit fracas.NotifyBase ()

        let status =    self |> fracas.mkField <@ self.Status @>    beacon
        let timestamp = self |> fracas.mkField <@ self.Timestamp @> timestamp

        member __.Age = DateTimeOffset.UtcNow - timestamp.Value

        member __.Status
            with get() = status.Value
            and set newValue = status.Value <- newValue

        member __.Timestamp
            with get() = timestamp.Value
            and set newValue = timestamp.Value <- newValue


    /// Log changes in beacon status.
    type IBeaconSupport<'B, 'K> =
        // Beacon management
        abstract GetKey :    'B -> 'K
        abstract IsChanged : old : 'B -> newer : 'B -> bool

        // Logging
        abstract Added :     'B -> unit
        abstract Expired :   'B -> DateTimeOffset -> unit
        abstract Replaced :  old : 'B -> newer : 'B -> unit

    type BeaconExpirationPolicy = RemoveExpiredBeacons | KeepExpiredBeacons

    module private BeaconsDetails =

        // Generalized listener for UDP beacons.
        type BeaconListener<'B>(beaconPort, mapfn : Udp.UdpReceived -> 'B option) =

            let disposed = ref false
            let packetSubject, packetListenerDisposable =
                Udp.makeUdpListener System.Net.IPAddress.Any beaconPort true

            let beaconSubject = new Subject<'B>()

            let packetSubscription = packetSubject.Subscribe(
                                        fun pkt ->
                                            let msg = mapfn pkt
                                            match msg with
                                            | Some msg -> beaconSubject.OnNext msg
                                            | None -> ())

            interface IDisposable with
                member __.Dispose() =
                    Dispose.theseWith disposed
                        [packetSubscription; packetListenerDisposable; beaconSubject]
                        (fun () -> beaconSubject.OnCompleted())

            member __.Beacons = beaconSubject :> IObservable<'B>

        let removeFromCollection (support : IBeaconSupport<'B, 'K>)
                                 key
                                 (beaconCollection : ObservableCollection<StatusHolder<'B>>) =
            let targets = query { for elem in beaconCollection do
                                  where (support.GetKey(elem.Status) = key)
                                  select elem }
                            |> Seq.toList // make concrete now to avoid collection changes while removing
            for elem in targets do
                beaconCollection.Remove(elem) |> ignore
                support.Expired elem.Status DateTimeOffset.Now

        let getBeaconInsertionIndex beacon getKey
                                    (beaconCollection : ObservableCollection<StatusHolder<'B>>) =

            let newKey = getKey beacon
            beaconCollection |> Seq.takeWhile (fun t -> newKey > getKey t.Status)
                             |> Seq.length

        let updateBeaconStatus (support : IBeaconSupport<'B, 'K>)
                               (beacon : 'B)
                               (beaconCollection : ObservableCollection<StatusHolder<'B>>)
                               (beaconTable : Dictionary<'K, DateTimeOffset * 'B>) =

            let now = DateTimeOffset.UtcNow

            let addOrUpdateStatusInCollection status =

                let getKey = support.GetKey

                let targets = query { for elem in beaconCollection do
                                        where (getKey elem.Status = getKey beacon)
                                        select elem }
                                |> Seq.toList
                match targets with
                | [] -> let idx = getBeaconInsertionIndex status getKey beaconCollection
                        beaconCollection.Insert(idx, StatusHolder(status, DateTimeOffset.UtcNow))
                        support.Added status
                | head :: tail ->
                    assert (tail.Length = 0)
                    let idx = beaconCollection.IndexOf(head)
                    beaconCollection.[idx].Timestamp <- now
                    beaconCollection.[idx].Status <- status

            let status = beacon
            let key = support.GetKey(beacon)

            // If a source changes IP address remove it from the collection & re-add.
            if beaconTable.ContainsKey(key) then
                let old = snd beaconTable.[key]
                if support.IsChanged old beacon then
                    support.Replaced  old beacon
                    removeFromCollection support key beaconCollection

            beaconTable.[key] <- (now, status)
            addOrUpdateStatusInCollection status

        let removeExpiredBeacons (support : IBeaconSupport<'B, 'K>)
                                 expiration
                                 beaconCollection
                                 filter
                                 (beaconTable : Dictionary<'K, DateTimeOffset * 'B>) =
            let expiredBeacons =
                query { for beacon in beaconTable do
                        where ((fst beacon.Value) < expiration && filter (snd beacon.Value))
                        select (support.GetKey(snd beacon.Value)) }
                |> Seq.toList // make concrete now to avoid collection changes while removing
            for key in expiredBeacons do
                let entry = beaconTable.[key]
                let time, beacon = entry
                support.Expired beacon time
                beaconTable.Remove(key) |> ignore
                removeFromCollection support key beaconCollection

        let removeAllExpiredBeacons (support : IBeaconSupport<'B, 'K>)
                                    expiration
                                    beaconCollection
                                    (beaconTable : Dictionary<'K, DateTimeOffset * 'B>) =

            removeExpiredBeacons support expiration beaconCollection
                                 (fun _ -> true) beaconTable

        type BeaconEvent<'B> =
            | Beacon of 'B
            | N of int64

        let updateBeaconTableEntries (support : IBeaconSupport<'B, 'K>)
                                     expirationPeriod beaconCollection beaconTable
                                     expirationPolicy ev =
            match ev with
            | Beacon beacon -> updateBeaconStatus support
                                                  beacon beaconCollection beaconTable
            | N _ ->
                let deadline = DateTimeOffset.Now - expirationPeriod
                match expirationPolicy with
                | RemoveExpiredBeacons ->
                    removeAllExpiredBeacons support deadline beaconCollection beaconTable
                | KeepExpiredBeacons -> ()

        let mkBeaconListener beaconPort (toBeacon : Udp.UdpReceived -> 'B option) =

            new BeaconListener<'B>(beaconPort, toBeacon)


    open BeaconsDetails
    open System.Reactive
    open System.Reactive.Linq

    /// Maintains an observable collection of beacons.
    type BeaconSource<'B, 'K when 'B : equality and 'K : comparison>
             (beaconPort, expirationPeriod, toBeacon, support,
              observationContext : SynchronizationContext, expirationPolicy) =

        let disposed = ref false
        let beaconSource = mkBeaconListener beaconPort toBeacon
        let beaconCollection = new ObservableCollection<StatusHolder<'B>>()
        let beaconTable = Dictionary<'K, DateTimeOffset * 'B>()
        let beaconSubject = new Subject<'B>()
        let stateGuard = Object()

        let timerPeriod = TimeSpan.FromSeconds(1.0)
        let timer = Observable.Interval(timerPeriod)

        let updateBeaconTable =
            BeaconsDetails.updateBeaconTableEntries
                            support expirationPeriod beaconCollection
                            beaconTable expirationPolicy
            
        // Handle beacons and timeouts
        let eventSubscription =
            Observable.Merge<BeaconEvent<'B>>([ timer.Select(fun n -> N n)
                                                beaconSource.Beacons.Select(fun b -> Beacon b) ])
                .ObserveOn(observationContext)
                .Subscribe(fun ev ->
                                lock stateGuard (fun () -> updateBeaconTable ev)
                                match ev with
                                | Beacon beacon -> beaconSubject.OnNext(beacon)
                                | N _ -> ())

        interface IDisposable with
            member __.Dispose() = Dispose.theseWith disposed
                                    [eventSubscription; beaconSource; beaconSubject]
                                    (fun () -> beaconSubject.OnCompleted())

        member s.Dispose() = (s :> IDisposable).Dispose()

        member __.BeaconCollection = beaconCollection
        member __.Beacons = beaconSubject :> IObservable<'B>

        /// Blocking wait for the beacon you're interested in, for F# folk.
        /// Cancellation does not throw. Returns Task<> to ensure equal behavior with C#.
        member s.WaitForBeaconAsync (predicate : 'B -> bool) (timeout : TimeSpan) : Async<'B option> = async {
            Log.Debug("BeaconSource.WaitForBeaconAsync: entering...")
            try
                let mutable beacon = None
                use ev = new ManualResetEventSlim(false)

                use observer = new AnonymousObserver<'B>(
                                    onNext = (fun b ->
                                                Log.Debug("BeaconSource.WaitForBeaconAsync: Received a beacon")
                                                if beacon.IsNone then
                                                    beacon <- Some b
                                                ev.Set () |> ignore),
                                    onError = fun _ -> ev.Set() |> ignore)
                let! ct = Async.CancellationToken

                Log.Debug("BeaconSource.WaitForBeaconAsync: Set up subscription")
                use _sub =
                    s.Beacons.Where(predicate)
                             .Timeout(timeout)
                             .SubscribeOn(observationContext)
                             .SubscribeSafe(observer)

                try
                    Log.Debug("BeaconSource.WaitForBeaconAsync: waiting...")
                    ev.Wait(-1, ct) |> ignore
                    Log.Debug("BeaconSource.WaitForBeaconAsync: wait complete.")
                    return beacon
                with
                    :? OperationCanceledException -> return None
            finally
                Log.Debug("BeaconSource.WaitForBeaconAsync: leaving.")
        }
