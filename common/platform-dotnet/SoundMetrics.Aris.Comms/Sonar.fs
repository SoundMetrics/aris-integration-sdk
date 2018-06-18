// Copyright 2014 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Serilog
open SoundMetrics.Aris.Config
open System
open System.Net
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks

[<AutoOpen>]
module private SonarLogging =

    module ArisLog =
        let logSonarDetected (sn : SerialNumber) (ipAddr : IPAddress) =
            Log.Information("Sonar {SerialNumber} detected at {IPAddress}", sn, ipAddr)

        let logSonarExpired (sn : int) (lastHeard : DateTimeOffset) (ipAddr : IPAddress) =
            Log.Information("Sonar {SerialNumber} expired from available table; last heard {LastHeader} on {IPAddr}",
                sn, lastHeard, ipAddr)

        let logSonarChangedAddress (sn : int) (addr1 : IPAddress) (addr2 : IPAddress) =
            Log.Information("Sonar {SerialNumber} changed IP address from {OldIPAddress} to {NewIPAddress}",
                sn, addr1, addr2)

    module DefenderLog =
        let logDefenderDetected (sn : SerialNumber) (ipAddr : IPAddress) =
            Log.Information("Defender {SerialNumber} detected at {IPAddress}", sn, ipAddr)

        let logDefenderExpired (sn : int) (lastHeard : DateTimeOffset) (ipAddr : IPAddress) =
            Log.Information("Defender {SerialNumber} expired from available table; last heard {LastHeader} on {IPAddr}",
                sn, lastHeard, ipAddr)

        let logDefenderChangedAddress (sn : int) (addr1 : IPAddress) (addr2 : IPAddress) =
            Log.Information("Defender {SerialNumber} changed IP address from {OldIPAddress} to {NewIPAddress}",
                sn, addr1, addr2)

    module CMLog =
        let logCMDetected (ipAddr : IPAddress) = Log.Information("CM detected at {IPAddress}", ipAddr)

        let logCMExpired (lastHeard : DateTimeOffset) (ipAddr : IPAddress) =
            Log.Information("CM expired from available table; last heard {LastHeard} on {IPAddress}",
                lastHeard, ipAddr)

//
// Beacon listener
//

// Sonar beacon

type AvailabilityState =
    | Available = 0
    | Busy = 1

type SoftwareVersion = { major: int; minor: int; buildNumber: int }
with
    override x.ToString() = sprintf "%d.%d.%d" x.major x.minor x.buildNumber

module internal SystemVariant =

    [<Literal>]
    let Voyager = "VG"

type SonarBeacon = {
    Timestamp :         DateTime
    SrcIpAddr :         IPAddress
    SerialNumber :      SerialNumber
    SystemType :        SystemType
    SoftwareVersion :   SoftwareVersion
    ConnectionState :   AvailabilityState
    CpuTemp :           float32 option
    IsDiverHeld :       bool
    IsVoyager :         bool
}

// Defender beacon

type OnBoardRecordState =
    | Ready = 0
    | Recording = 1

type OnBoardStorageState =
    | Nominal = 0
    | StorageFull = 1
    | StorageError = 2
    | StorageMissing = 3

type OnBoardBatteryState =
    | Nominal = 0
    | Low = 1
    | NoPower = 2
    | Missing = 3
    | OnTetherPower = 4

type DefenderBeacon = {
    Timestamp: DateTime
    SrcIpAddr: IPAddress
    SerialNumber: SerialNumber
    SystemType: SystemType
    SoftwareVersion: SoftwareVersion
    ConnectionState: AvailabilityState
    RecordState : OnBoardRecordState
    StorageState : OnBoardStorageState
    StorageLevel : float32
    BatteryState : OnBoardBatteryState
    BatteryLevel : float32
}

// Command Module Beacon

type internal CommandModuleBeacon = {
    SrcIpAddr : IPAddress

    // sonar_serial_number[] is not currently supported
    ArisCurrent : float32 option
    ArisPower : float32 option
    ArisVoltage : float32 option
    CpuTemp : float32 option
    Revision : uint32 option
}
with
    static member inline ValueOrNone<'T when 'T : equality> (v : 'T)
        = if (v = Unchecked.defaultof<'T>) then None else Some v

    static member internal From(pkt : Udp.UdpReceived) =

        let cms = Aris.CommandModuleBeacon.Parser.ParseFrom(pkt.udpResult.Buffer)
        {
            SrcIpAddr = pkt.udpResult.RemoteEndPoint.Address
            ArisCurrent =   CommandModuleBeacon.ValueOrNone cms.ArisCurrent
            ArisPower =     CommandModuleBeacon.ValueOrNone cms.ArisPower
            ArisVoltage =   CommandModuleBeacon.ValueOrNone cms.ArisVoltage
            CpuTemp =       CommandModuleBeacon.ValueOrNone cms.CpuTemp
            Revision =      CommandModuleBeacon.ValueOrNone cms.Revision
        }


type IBeaconSourceCallbacks<'B> =
    abstract Added :     'B -> unit
    abstract Expired :   'B -> DateTimeOffset -> unit
    abstract Replaced :  old : 'B -> newer : 'B -> unit

//
// Sonar
//

type Sonar = { SerialNumber: SerialNumber; SystemType: SystemType }

//
// Available sonar status
//

type AvailableSonarStatus = {
    beacon: SonarBeacon
}

/// Mutable wrapper that allows us to update the status without causing notification
/// of the observable collection changing.
type StatusHolder = { mutable status: AvailableSonarStatus }

module BeaconListeners =
    type SonarAvailability  = Aris.Availability
    type DefenderAvailability = Defender.Availability
    type CMBeacon = Aris.CommandModuleBeacon

    open Beacons

    /// Helper functions for C# code.
    [<CompiledName("NoCallbacks")>]
    let noCallbacks<'B>() = Option<IBeaconSourceCallbacks<'B>>.None

    // Combines the internal IBeaconSupport<> interface with the public
    // IBeaconSourceCallbacks<> so we can treat them as one callback interface.
    let private combineNotifications<'B, 'K> (support : IBeaconSupport<'B, 'K>)
                                             (callbacks : IBeaconSourceCallbacks<'B>)
                                             : IBeaconSupport<'B, 'K> =

        { new IBeaconSupport<'B, 'K> with

            member __.GetKey beacon = support.GetKey(beacon)
            member __.IsChanged old newer = support.IsChanged old newer

            member __.Added beacon = support.Added(beacon)
                                     callbacks.Added(beacon)

            member __.Expired beacon lastUpdate = support.Expired beacon lastUpdate
                                                  callbacks.Expired beacon lastUpdate

            member __.Replaced old newer = support.Replaced old newer
                                           callbacks.Replaced old newer
        }


    /// Listens for and reports ARIS beacons on the network.
    [<CompiledName("CreateSonarBeaconListener")>]
    let createSonarBeaconListener expirationPeriod observationContext beaconExpirationPolicy
                                  callbacks : BeaconSource<SonarBeacon, SerialNumber> =

        let toSoftwareVersion (ver: SonarAvailability.Types.SoftwareVersion) =
            { major = int ver.Major; minor = int ver.Minor; buildNumber = int ver.Buildnumber }

        let toBeacon (pkt : Udp.UdpReceived) =
            try
                let av = SonarAvailability.Parser.ParseFrom(pkt.udpResult.Buffer)
                let beacon =
                    { Timestamp = pkt.timestamp
                      SrcIpAddr = pkt.udpResult.RemoteEndPoint.Address
                      SerialNumber = int av.SerialNumber
                      SystemType = enum (int av.SystemType)
                      SoftwareVersion = toSoftwareVersion av.SoftwareVersion
                      ConnectionState = enum (int av.ConnectionState)
                      CpuTemp = Some(av.CpuTemp)
                      IsDiverHeld = av.IsDiverHeld
                      IsVoyager = av.SystemVariants.Enabled |> Seq.contains SystemVariant.Voyager
                    }
                Some beacon
            with
                _ -> None

        let support = {
            new IBeaconSupport<SonarBeacon, SerialNumber> with

                // Beacon management

                member __.GetKey beacon = beacon.SerialNumber

                member __.IsChanged old newer = old.SrcIpAddr <> newer.SrcIpAddr

                // Logging

                member __.Added beacon =

                    ArisLog.logSonarDetected beacon.SerialNumber beacon.SrcIpAddr

                member __.Expired beacon lastUpdate =

                    ArisLog.logSonarExpired beacon.SerialNumber lastUpdate beacon.SrcIpAddr

                member __.Replaced old newer =

                    ArisLog.logSonarChangedAddress old.SerialNumber old.SrcIpAddr newer.SrcIpAddr
        }

        let combinedNotifications = match callbacks with
                                    | Some callbacks -> combineNotifications support callbacks
                                    | None -> support

        new BeaconSource<SonarBeacon, SerialNumber>(
                    NetworkConstants.SonarAvailabilityListenerPortV2, expirationPeriod, toBeacon, combinedNotifications,
                    observationContext, beaconExpirationPolicy)

    /// Listens for and reports ARIS beacons on the network, using reasonable default settings.
    [<CompiledName("CreateDefaultSonarBeaconListener")>]
    let createDefaultSonarBeaconListener observationContext : BeaconSource<SonarBeacon, SerialNumber> =
        createSonarBeaconListener (TimeSpan.FromSeconds(10.0)) observationContext
            BeaconExpirationPolicy.RemoveExpiredBeacons None

    /// Listens for and reports ARIS Defender beacons on the network.
    [<CompiledName("CreateDefenderBeaconListener")>]
    let internal createDefenderBeaconListener expirationPeriod observationContext beaconExpirationPolicy
                                 callbacks : BeaconSource<DefenderBeacon, SerialNumber> =

        let toSoftwareVersion (ver: DefenderAvailability.Types.SoftwareVersion) =
            { major = int ver.Major; minor = int ver.Minor; buildNumber = int ver.Buildnumber }

        let toBeacon (pkt : Udp.UdpReceived) =
            try
                let av = DefenderAvailability.Parser.ParseFrom(pkt.udpResult.Buffer)
                let beacon =
                    { Timestamp = pkt.timestamp
                      SrcIpAddr = pkt.udpResult.RemoteEndPoint.Address
                      SerialNumber = int av.SerialNumber
                      SystemType = enum (int av.SystemType)
                      SoftwareVersion = toSoftwareVersion av.SoftwareVersion
                      ConnectionState = enum (int av.ConnectionState)
                      RecordState =  enum (int av.RecordState)
                      StorageState = enum (int av.StorageState)
                      StorageLevel = av.StorageLevel
                      BatteryState = enum (int av.BatteryState)
                      BatteryLevel = av.BatteryLevel }
                Some beacon
            with
                _ -> None

        let support = {
            new IBeaconSupport<DefenderBeacon, SerialNumber> with

                // Beacon management

                member __.GetKey beacon = beacon.SerialNumber

                member __.IsChanged old newer = old.SrcIpAddr <> newer.SrcIpAddr

                // Logging

                member __.Added beacon =

                    DefenderLog.logDefenderDetected beacon.SerialNumber beacon.SrcIpAddr

                member __.Expired beacon lastUpdate =

                    DefenderLog.logDefenderExpired beacon.SerialNumber lastUpdate beacon.SrcIpAddr

                member __.Replaced old newer =

                    DefenderLog.logDefenderChangedAddress old.SerialNumber old.SrcIpAddr newer.SrcIpAddr
        }

        let combinedNotifications = match callbacks with
                                    | Some callbacks -> combineNotifications support callbacks
                                    | None -> support

        new BeaconSource<DefenderBeacon, SerialNumber>(
                    NetworkConstants.DefenderBeaconPort, expirationPeriod, toBeacon, combinedNotifications,
                    observationContext, beaconExpirationPolicy)


    /// Listens for and reports ARIS Defender beacons on the network, using reasonable default settings.
    [<CompiledName("CreateDefaultDefenderBeaconListener")>]
    let internal createDefaultDefenderBeaconListener observationContext : BeaconSource<DefenderBeacon, SerialNumber> =
        createDefenderBeaconListener (TimeSpan.FromSeconds(10.0)) observationContext
            BeaconExpirationPolicy.RemoveExpiredBeacons None

    /// Listens for and reports ARIS Defender beacons on the network.
    let internal mkCommandModuleBeaconListener beaconPort expirationPeriod observationContext beaconExpirationPolicy
                                      callbacks : BeaconSource<CommandModuleBeacon, int64> =

        let toBeacon (pkt : Udp.UdpReceived) =
            try
                let beacon = CommandModuleBeacon.From(pkt)
                Some beacon
            with
                _ -> None

        let support = {
            // CM doesn't have a serial number to put in the beacon so
            // use the IP address for a key (converted to an int64).
            new IBeaconSupport<CommandModuleBeacon, int64> with

                // Beacon management

                member __.GetKey beacon =

                    let bytes = beacon.SrcIpAddr.GetAddressBytes()
                    if bytes.Length = 4 then
                        int64 (BitConverter.ToInt32(bytes, 0))
                    else
                        BitConverter.ToInt64(bytes, 0)

                member __.IsChanged old newer =
                
                    old.SrcIpAddr <> newer.SrcIpAddr || old.Revision <> newer.Revision

                // Logging

                member __.Added beacon =

                    CMLog.logCMDetected beacon.SrcIpAddr

                member __.Expired beacon lastUpdate =

                    CMLog.logCMExpired lastUpdate beacon.SrcIpAddr

                member __.Replaced old newer =

                    () // No identity on the CM, so this is pretty meaningless.
        }

        let combinedNotifications = match callbacks with
                                    | Some callbacks -> combineNotifications support callbacks
                                    | None -> support

        new BeaconSource<CommandModuleBeacon, int64>(
                    beaconPort, expirationPeriod, toBeacon, combinedNotifications,
                    observationContext, beaconExpirationPolicy)

type AvailableSonars = Beacons.BeaconSource<SonarBeacon, SerialNumber>
type internal AvailableDefenders = Beacons.BeaconSource<DefenderBeacon, SerialNumber>
type internal AvailableCommandModules = Beacons.BeaconSource<CommandModuleBeacon, int64>

module private BeaconExtensions =
    let matchExplorerBeacon sn = fun (b : SonarBeacon) -> b.SerialNumber = sn
    let matchDefenderBeacon sn = fun (b : DefenderBeacon) -> b.SerialNumber = sn

[<Extension>]
type BeaconExtensions =

    /// Extension method to fetch an option's value.
    [<Extension>]
    static member ToValue<'T>(option : Option<'T>, value : 'T byref) : bool =
        match option with
        | Some t -> value <- t
                    true
        | None -> false

    /// Extension method to wait on an Explorer beacon by serial number.
    [<Extension>]
    static member WaitForExplorerBySerialNumberAsync (availables : AvailableSonars,
                                                      sn : SerialNumber,
                                                      ct : CancellationToken) : Task<SonarBeacon option> =
        availables.WaitForBeaconAsync(BeaconExtensions.matchExplorerBeacon sn, ct)

    /// Extension method to wait on an Explorer beacon by serial number, with timeout.
    [<Extension>]
    static member WaitForExplorerBySerialNumberAsync (availables : AvailableSonars,
                                                      sn : SerialNumber,
                                                      timeout : TimeSpan) : Task<SonarBeacon option> =
        use cts = new CancellationTokenSource(timeout)
        availables.WaitForExplorerBySerialNumberAsync(sn, cts.Token)

    /// Extension method to wait on a Defender beacon by serial number.
    [<Extension>]
    static member WaitForDefenderBySerialNumberAsync (availables : AvailableDefenders,
                                                      sn : SerialNumber,
                                                      ct : CancellationToken) : Task<DefenderBeacon option> =
        availables.WaitForBeaconAsync(BeaconExtensions.matchDefenderBeacon sn, ct)

    /// Extension method to wait on a Defender beacon by serial number, with timeout.
    [<Extension>]
    static member WaitForDefenderBySerialNumberAsync (availables : AvailableDefenders,
                                                      sn : SerialNumber,
                                                      timeout : TimeSpan) : Task<DefenderBeacon option> =
        use cts = new CancellationTokenSource(timeout)
        availables.WaitForDefenderBySerialNumberAsync(sn, cts.Token)
