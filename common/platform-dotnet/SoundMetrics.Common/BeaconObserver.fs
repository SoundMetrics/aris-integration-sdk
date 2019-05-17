// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open SoundMetrics.Network
open System
open System.Net
open System.Reactive.Subjects

module internal BeaconObserverDetails =

    let private mkBeaconObserver<'B> (port : int)
                                     (mapPktToBeacon : UdpReceived -> 'B option)
                                     : IDisposable * ISubject<'B> =

        let listener = new UdpListener(IPAddress.Any,
                                       port,
                                       reuseAddr = true)
        let subject = new Subject<'B>()

        let subscription =
            listener.Packets
                    .Subscribe(fun pkt ->
                        match mapPktToBeacon pkt with
                        | Some beacon -> subject.OnNext(beacon)
                        | None -> () )

        let disposable =
            { new IDisposable with
                member __.Dispose() =
                    subscription.Dispose()
                    listener.Dispose()
                    subject.OnCompleted()
                    subject.Dispose()
            }

        disposable, subject :> ISubject<'B>


    let mkExplorerBeaconObserver () : IDisposable * ISubject<ArisBeacon> =

        mkBeaconObserver<ArisBeacon>
            NetworkConstants.ArisAvailabilityListenerPortV2
            BeaconListener.toArisExplorerOrVoyagerBeacon


    let mkCommandModuleBeaconObserver () : IDisposable * ISubject<ArisCommandModuleBeacon> =

        mkBeaconObserver<ArisCommandModuleBeacon>
            NetworkConstants.ArisCommandModuleBeaconPort
            BeaconListener.toArisCommandModuleBeacon

type BeaconObserver<'B> internal (disposer : IDisposable, subject : ISubject<'B>) =

    let mutable disposed = false

    let dispose () =

        if not disposed then
            disposed <- true
            disposer.Dispose()

    interface IDisposable with
        member __.Dispose() = dispose()

    member __.Beacons = subject :> IObservable<'B>

module BeaconObserver =

    [<CompiledName("CreateExplorerBeaconObserver")>]
    let mkExplorerBeaconObserver () =
        new BeaconObserver<ArisBeacon>(
            BeaconObserverDetails.mkExplorerBeaconObserver())

    [<CompiledName("CreateCommandModuleBeaconObserver")>]
    let mkCommandModuleBeaconObserver () =
        new BeaconObserver<ArisCommandModuleBeacon>(
            BeaconObserverDetails.mkCommandModuleBeaconObserver())
