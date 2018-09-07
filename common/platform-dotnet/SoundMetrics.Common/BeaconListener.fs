// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open ArisBeaconDetails
open ArisCommandModuleDetails
open System

module internal BeaconListener =
    open System.Collections.ObjectModel
    open Udp

    type SonarAvailability  = Aris.Availability
    type DefenderAvailability = Defender.Availability
    type CMBeacon = Aris.CommandModuleBeacon
    type ProtobufSystemType = Aris.Availability.Types.SystemType

    let toSoftwareVersion (ver: SonarAvailability.Types.SoftwareVersion) =
        { Major = int ver.Major; Minor = int ver.Minor; BuildNumber = int ver.Buildnumber }

    let toDefenderSoftwareVersion (ver: DefenderAvailability.Types.SoftwareVersion) =
        { Major = int ver.Major; Minor = int ver.Minor; BuildNumber = int ver.Buildnumber }

    let toArisExplorerOrVoyagerBeacon (pkt : Udp.UdpReceived) : ArisBeacon option =
        try
            let av = SonarAvailability.Parser.ParseFrom(pkt.UdpResult.Buffer)
            let beacon =
                let model =
                    defaultArg
                        (av.SystemVariants // possibly null
                            |> Option.ofObj
                            |> Option.map (fun variants ->
                                if variants.Enabled |> Seq.contains VoyagerVariant then
                                    Voyager
                                else
                                    Explorer
                            ))
                        Explorer

                {
                    Model =             model
                    SystemType =        enum (int av.SystemType)
                    SerialNumber =      av.SerialNumber
                    SoftwareVersion =   toSoftwareVersion av.SoftwareVersion
                    Timestamp =         pkt.Timestamp
                    IPAddress =         pkt.UdpResult.RemoteEndPoint.Address
                    ConnectionState =   enum (int av.ConnectionState)
                    CpuTemp =           av.CpuTemp
                }
            Some beacon
        with
            _ -> None

    let toArisDefenderBeacon (pkt : Udp.UdpReceived) : ArisBeacon option =
        try
            let av = DefenderAvailability.Parser.ParseFrom(pkt.UdpResult.Buffer)
            let defenderState = {
                RecordState =   enum (int av.RecordState)
                StorageState =  enum (int av.StorageState)
                StorageLevel =  av.StorageLevel
                BatteryState =  enum (int av.BatteryState)
                BatteryLevel =  av.BatteryLevel
            }

            let beacon =
                {
                    Model =             Defender defenderState
                    SystemType =    enum (int av.SystemType)
                    SerialNumber =      av.SerialNumber
                    SoftwareVersion =   toDefenderSoftwareVersion av.SoftwareVersion
                    Timestamp =         pkt.Timestamp
                    IPAddress =         pkt.UdpResult.RemoteEndPoint.Address
                    ConnectionState =   enum (int av.ConnectionState)
                    CpuTemp =           Single.NaN
                }
            Some beacon
        with
            _ -> None

    let toArisCommandModuleBeacon (pkt : Udp.UdpReceived) : ArisCommandModuleBeacon option =

        try
            let cms = Aris.CommandModuleBeacon.Parser.ParseFrom(pkt.UdpResult.Buffer)
            Some {
                IPAddress = pkt.UdpResult.RemoteEndPoint.Address
                ArisCurrent =   cms.ArisCurrent
                ArisPower =     cms.ArisPower
                ArisVoltage =   cms.ArisVoltage
                CpuTemp =       cms.CpuTemp
                Revision =      cms.Revision
                Timestamp =     pkt.Timestamp
            }
        with
            _ -> None


    let inline addOrUpdateBeacon (beacons : ObservableCollection<'B>)
                                 (isSame : 'B -> 'B -> bool)
                                 (isLess : 'B -> 'B -> bool)
                                 (updateB : 'B) =
        // N is small, so just operate linearly.
        let indexedBeacons = beacons |> Seq.mapi (fun i b -> i, b) |> Seq.cache

        match indexedBeacons |> Seq.tryFind (fun (_, b) -> isSame b updateB) with
        | Some (i, _) -> beacons.[i] <- updateB
        | None ->
            match indexedBeacons |> Seq.tryFind (fun (_, b) -> isLess updateB b) with
            | Some (i, _) -> beacons.Insert(i, updateB)
            | None -> beacons.Add(updateB)

    let processExplorerBeaconPacket (collection : ObservableCollection<ArisBeacon>)
                                    (shouldInclude : NetworkDevice -> bool)
                                    (pkt : UdpReceived) =

            match toArisExplorerOrVoyagerBeacon pkt with
            | Some beacon ->
                let device = Aris beacon
                if shouldInclude device then
                    addOrUpdateBeacon collection
                        (fun b b' -> b.SerialNumber = b'.SerialNumber)
                        (fun b b' -> b.SerialNumber < b'.SerialNumber)
                        beacon
                    Some device
                else
                    None
            | None -> None

    let processDefenderBeaconPacket (collection : ObservableCollection<ArisBeacon>)
                                    (shouldInclude : NetworkDevice -> bool)
                                    (pkt : UdpReceived) =

            match toArisDefenderBeacon pkt with
            | Some beacon ->
                let device = Aris beacon
                if shouldInclude device then
                    addOrUpdateBeacon collection
                        (fun b b' -> b.SerialNumber = b'.SerialNumber)
                        (fun b b' -> b.SerialNumber < b'.SerialNumber)
                        beacon
                    Some device
                else
                    None
            | None -> None

    let processCommandModuleBeaconPacket (collection : ObservableCollection<ArisCommandModuleBeacon>)
                                         (pkt : UdpReceived) =

            match toArisCommandModuleBeacon pkt with
            | Some beacon ->
                addOrUpdateBeacon collection
                    (fun b b' -> b.IPAddress = b'.IPAddress)
                    (fun b b' -> b.IPAddress.ToString() < b'.IPAddress.ToString())
                    beacon
                Some (ArisCommandModule beacon)
            | None -> None

    let inline cleanCollection (collection : ObservableCollection<'T>) (isExpired : 'T -> bool) =

        for idx = collection.Count - 1 downto 0 do
            if isExpired collection.[idx] then
                collection.RemoveAt(idx)

    let cleanArisCollection (collection : ObservableCollection<ArisBeacon>) expirationPeriod =

        let now = DateTime.Now
        let isExpired (beacon : ArisBeacon) = beacon.Timestamp + expirationPeriod < now
        cleanCollection collection isExpired

    let cleanCMCollection (collection : ObservableCollection<ArisCommandModuleBeacon>) expirationPeriod =

        let now = DateTime.Now
        let isExpired (beacon : ArisCommandModuleBeacon) = beacon.Timestamp + expirationPeriod < now
        cleanCollection collection isExpired


// ----------------------------------------------------------------------------

open System.Collections.ObjectModel
open System.Net
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks
open BeaconListener
open Serilog
open Udp

/// Provides access to the beacons of various Sound Metrics devices.
/// See <see cref="CreateForArisExplorerAndVoyager"/> and
/// <see cref="CreateForArisDefender"/> for simple construction.
[<Sealed>]
type BeaconListener (expirationPeriod : TimeSpan, filter : Func<NetworkDevice, bool>) as self =
    // ctor(Func<_,_>) is the primary ctor as it helps on the C# side.

    let shouldInclude device = filter.Invoke(device)
    let syncContext =   let ctx = SynchronizationContext.Current
                        if isNull ctx then
                            failwith "No SynchronizationContext is set"
                        ctx

    let arisExplorerCollection = ObservableCollection<ArisBeacon>()
    let arisDefenderCollection = ObservableCollection<ArisBeacon>()
    let arisCommandModuleCollection = ObservableCollection<ArisCommandModuleBeacon>()
    let beaconSubject = new Subject<NetworkDevice>()

    // Listen for ARIS Explorers
    let explorerListener = new UdpListener(IPAddress.Any,
                                           NetworkConstants.ArisAvailabilityListenerPortV2,
                                           reuseAddr = true)
    let explorerSub = explorerListener.Packets
                        .ObserveOn(syncContext)
                        .Subscribe(fun pkt ->
                            let callback = fun _ ->
                                match processExplorerBeaconPacket arisExplorerCollection shouldInclude pkt with
                                | Some device ->
                                    if beaconSubject.HasObservers then
                                        beaconSubject.OnNext(device)
                                | None -> ()
                            syncContext.Post(SendOrPostCallback callback, ()) )

    // Listen for ARIS Defenders
    let defenderListener = new UdpListener(IPAddress.Any,
                                           NetworkConstants.ArisDefenderBeaconPort,
                                           reuseAddr = true)
    let defenderSub = defenderListener.Packets.Subscribe(fun pkt ->
                            let callback = fun _ ->
                                match processDefenderBeaconPacket arisDefenderCollection shouldInclude pkt with
                                | Some device -> beaconSubject.OnNext(device)
                                | None -> ()
                            syncContext.Post(SendOrPostCallback callback, ()) )

    // Listen for ARIS Command Modules
    let cmListener = new UdpListener(IPAddress.Any,
                                     NetworkConstants.ArisCommandModuleBeaconPort,
                                     reuseAddr = true)
    let cmSub = cmListener.Packets.Subscribe(fun pkt ->
                            let callback = fun _ ->
                                match processCommandModuleBeaconPacket arisCommandModuleCollection pkt with
                                | Some device -> beaconSubject.OnNext(device)
                                | None -> ()
                            syncContext.Post(SendOrPostCallback callback, ()) )

    // Weed out expired beacons
    let weederSub =
        let cleanCollections () =
            cleanArisCollection arisExplorerCollection expirationPeriod
            cleanArisCollection arisDefenderCollection expirationPeriod
            cleanCMCollection arisCommandModuleCollection expirationPeriod
        Observable.Interval(TimeSpan.FromSeconds(5.0))
                           .ObserveOn(syncContext)
                           .Subscribe(fun _ -> cleanCollections())

    let waitForArisByAsync (predicate : NetworkDevice -> bool) (timeout : TimeSpan) : Async<ArisBeacon option> = async {
        let! device = self.WaitForBeaconAsync predicate timeout
        return
            match device with
            | Some device ->
                match device with
                | Aris beacon -> Some beacon
                | _ -> None
            | _ -> None
    }

    let dispose disposing =

        if disposing then
            // Clean up managed resources
            beaconSubject.OnCompleted()
            let disposables : IDisposable list = [
                weederSub
                explorerSub; explorerListener
                defenderSub; defenderListener
                cmSub; cmListener
                beaconSubject
            ]
            disposables |> List.iter (fun d -> if d <> null then d.Dispose())

        // Clean up unmanaged resources
        ()

    interface IDisposable with
        override bl.Dispose() = dispose true
                                GC.SuppressFinalize(bl)

    member bl.Dispose() = (bl :> IDisposable).Dispose()
    override __.Finalize() = dispose false

    /// Access by subscription of all Sound Metrics devices.
    /// <seealso cref="NetworkDevice"/>
    member __.AllBeacons = beaconSubject :> IObservable<NetworkDevice>

    /// An observable collection of ARIS Explorer and ARIS Voyager beacons.
    member __.ArisExplorerBeacons = arisExplorerCollection

    /// An observable collection of ARIS Defender beacons.
    member __.ArisDefenderBeacons = arisDefenderCollection

    member internal __.ArisCommandModuleBeacons = arisCommandModuleCollection

    /// Factory function to create a beacon listener that sees only ARIS Explorer
    /// and ARIS Voyager beacons.
    static member CreateForArisExplorerAndVoyager (expirationPeriod : TimeSpan) =

        let predicate = function
            | Aris beacon -> match beacon.Model with
                             | Explorer | Voyager -> true
                             | _ -> false
            | _ -> false
        new BeaconListener(expirationPeriod, Func<_,_>(predicate))

    /// Factory function to create a beacon listener that sees only ARIS Defender beacons.
    static member CreateForArisDefender (expirationPeriod : TimeSpan) =

        let predicate = function
            | Aris beacon -> match beacon.Model with
                             | Defender _ -> true
                             | _ -> false
            | _ -> false
        new BeaconListener(expirationPeriod, Func<_,_>(predicate))


    /// Blocking wait for the beacon you're interested in, for F# folk.
    /// Cancellation does not throw.
    [<CompiledName("WaitForBeaconFSharpAsync")>]
    member s.WaitForBeaconAsync (predicate : NetworkDevice -> bool) (timeout : TimeSpan) : Async<NetworkDevice option> = async {
        Log.Debug("BeaconSource.WaitForBeaconAsync: entering...")
        try
            let mutable beacon = None
            use ev = new ManualResetEventSlim(false)

            use observer = new AnonymousObserver<NetworkDevice>(
                                onNext = (fun b ->
                                            Log.Debug("BeaconSource.WaitForBeaconAsync: Received a beacon")
                                            if beacon.IsNone then
                                                beacon <- Some b
                                            ev.Set () |> ignore),
                                onError = fun ex -> match ex with
                                                    | :? TimeoutException ->
                                                        Log.Information("Timed out waiting for beacon")
                                                    | _ -> Log.Information("Errored out while waiting for beacon: {msg}", ex.Message)
                                                    ev.Set() |> ignore)
            let! ct = Async.CancellationToken

            Log.Debug("BeaconSource.WaitForBeaconAsync: Set up subscription")
            use _sub =
                s.AllBeacons.Where(predicate)
                            .Timeout(timeout)
                            .ObserveOn(syncContext)
                            .SubscribeSafe(observer)

            try
                Log.Debug("BeaconSource.WaitForBeaconAsync: waiting...")
                if not (ev.Wait(-1, ct)) then
                    Log.Information("Timed out waiting for beacon")
                if ct.IsCancellationRequested then
                    Log.Information("Cancellation requested while waiting for beacon")
                Log.Debug("BeaconSource.WaitForBeaconAsync: wait complete.")
                return beacon
            with
                :? OperationCanceledException -> return None
        finally
            Log.Debug("BeaconSource.WaitForBeaconAsync: leaving.")
    }

    /// Blocking wait for the beacon you're interested in, for C# folk.
    /// Cancellation does not throw.
    [<CompiledName("WaitForBeaconAsync")>]
    member s.WaitForBeaconCSharpAsync (predicate : NetworkDevice -> bool, timeout : TimeSpan) : Task<NetworkDevice> =

        Async.StartAsTask(async {
            let! device = s.WaitForBeaconAsync predicate timeout
            return
                match device with
                | Some device -> device
                | None -> Unchecked.defaultof<NetworkDevice> // null for the C# user.
        })

    [<CompiledName("WaitForArisBySerialNumberFSharpAsync")>]
    member __.WaitForArisBySerialNumberAsync (sn : ArisSerialNumber) (timeout : TimeSpan) : Async<ArisBeacon option> =

        let matchesSN = function
            | Aris beacon -> beacon.SerialNumber = sn
            | _ -> false

        waitForArisByAsync matchesSN timeout

    [<CompiledName("WaitForArisBySerialNumberAsync")>]
    member s.WaitForArisBySerialNumberCSharpAsync (sn : ArisSerialNumber, timeout : TimeSpan) : Task<ArisBeacon> =

        Async.StartAsTask(async {
            let! beacon = s.WaitForArisBySerialNumberAsync sn timeout
            return
                match beacon with
                | Some beacon -> beacon
                | None -> Unchecked.defaultof<ArisBeacon> // null for the C# user.
        })

    [<CompiledName("WaitForArisByIPAddressFSharpAsync")>]
    member __.WaitForArisByIPAddressAsync (ipAddr : IPAddress) (timeout : TimeSpan) : Async<ArisBeacon option> =

        let matchesIP = function
            | Aris beacon -> (beacon.IPAddress = ipAddr)
            | _ -> false

        waitForArisByAsync matchesIP timeout

    [<CompiledName("WaitForArisByIPAddressAsync")>]
    member s.WaitForArisByIPAddressCSharpAsync (ipAddr : IPAddress, timeout : TimeSpan) : Task<ArisBeacon> =

        Async.StartAsTask(async {
            let! beacon = s.WaitForArisByIPAddressAsync ipAddr timeout
            return
                match beacon with
                | Some beacon -> beacon
                | None -> Unchecked.defaultof<ArisBeacon> // null for the C# user.
        })
