// Copyright 2014 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Serilog
open System
open System.Net

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
        let logSonarDetected (sn : SerialNumber) (ipAddr : IPAddress) =
            Log.Information("Defender {SerialNumber} detected at {IPAddress}", sn, ipAddr)

        let logSonarExpired (sn : int) (lastHeard : DateTimeOffset) (ipAddr : IPAddress) =
            Log.Information("Defender {SerialNumber} expired from available table; last heard {LastHeader} on {IPAddr}",
                sn, lastHeard, ipAddr)

        let logSonarChangedAddress (sn : int) (addr1 : IPAddress) (addr2 : IPAddress) =
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

module SystemVariant =

    [<Literal>]
    let Voyager = "VG"

type SonarBeacon = {
    timestamp: DateTime
    srcIpAddr: IPAddress
    serialNumber: SerialNumber
    systemType: SystemType
    softwareVersion: SoftwareVersion
    connectionState: AvailabilityState
    cpuTemp: float32 option
    isDiverHeld: bool
    isVoyager: bool
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
    timestamp: DateTime
    srcIpAddr: IPAddress
    serialNumber: SerialNumber
    systemType: SystemType
    softwareVersion: SoftwareVersion
    connectionState: AvailabilityState
    recordState : OnBoardRecordState option
    storageState : OnBoardStorageState option
    storageLevel : float32 option
    batteryState : OnBoardBatteryState option
    batteryLevel : float32 option
}

// Command Module Beacon

type CommandModuleBeacon = {
    SrcIpAddr : IPAddress

    // sonar_serial_number[] is not currently supported
    ArisCurrent : float32 option
    ArisPower : float32 option
    ArisVoltage : float32 option
    CpuTemp : float32 option
    Revision : uint32 option
}
with
    static member From(pkt : Udp.UdpReceived) =

        let cms = Aris.ProtocolMessages.CommandModule.CommandModuleBeacon.ParseFrom(pkt.udpResult.Buffer)
        {
            SrcIpAddr = pkt.udpResult.RemoteEndPoint.Address
            ArisCurrent =   if cms.HasArisCurrent       then Some cms.ArisCurrent       else None
            ArisPower =     if cms.HasArisPower         then Some cms.ArisPower         else None
            ArisVoltage =   if cms.HasArisVoltage       then Some cms.ArisVoltage       else None
            CpuTemp =       if cms.HasCpuTemp           then Some cms.CpuTemp           else None
            Revision =      if cms.HasRevision          then Some cms.Revision          else None
        }


type IBeaconSourceCallbacks<'B> =
    abstract Added :     'B -> unit
    abstract Expired :   'B -> DateTimeOffset -> unit
    abstract Replaced :  old : 'B -> newer : 'B -> unit

//
// Sonar
//

type Sonar = { serialNumber: SerialNumber; systemType: SystemType }

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
    type SonarAvailability  = Aris.ProtocolMessages.Sonar.Availability
    type DefenderAvailability = Aris.ProtocolMessages.DefenderAvailability.Availability
    type CMBeacon = Aris.ProtocolMessages.CommandModule.CommandModuleBeacon

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
    let mkSonarBeaconListener beaconPort expirationPeriod observationContext beaconExpirationPolicy
                              callbacks =

        let toSoftwareVersion (ver: SonarAvailability.Types.SoftwareVersion) =
            { major = int ver.Major; minor = int ver.Minor; buildNumber = int ver.Buildnumber }

        let toBeacon (pkt : Udp.UdpReceived) =
            try
                let av = SonarAvailability.ParseFrom(pkt.udpResult.Buffer)
                let beacon =
                    { timestamp = pkt.timestamp
                      srcIpAddr = pkt.udpResult.RemoteEndPoint.Address
                      serialNumber = int av.SerialNumber
                      systemType = enum (int av.SystemType)
                      softwareVersion = toSoftwareVersion av.SoftwareVersion
                      connectionState = enum (int av.ConnectionState)
                      cpuTemp = Some(av.CpuTemp)
                      isDiverHeld = av.HasIsDiverHeld && av.IsDiverHeld
                      isVoyager = av.HasSystemVariants
                                    && av.SystemVariants.EnabledList |> Seq.contains SystemVariant.Voyager
                    }
                Some beacon
            with
                _ -> None

        let support = {
            new IBeaconSupport<SonarBeacon, SerialNumber> with

                // Beacon management

                member __.GetKey beacon = beacon.serialNumber

                member __.IsChanged old newer = old.srcIpAddr <> newer.srcIpAddr

                // Logging

                member __.Added beacon =

                    arisLog.SonarDetected beacon.serialNumber beacon.srcIpAddr

                member __.Expired beacon lastUpdate =

                    arisLog.SonarExpired beacon.serialNumber lastUpdate beacon.srcIpAddr

                member __.Replaced old newer =

                    arisLog.SonarChangedAddress old.serialNumber old.srcIpAddr newer.srcIpAddr
        }

        let combinedNotifications = match callbacks with
                                    | Some callbacks -> combineNotifications support callbacks
                                    | None -> support

        new BeaconSource<SonarBeacon, SerialNumber>(
                    beaconPort, expirationPeriod, toBeacon, combinedNotifications,
                    observationContext, beaconExpirationPolicy)

    /// Listens for and reports ARIS Defender beacons on the network.
    let mkDefenderBeaconListener beaconPort expirationPeriod observationContext beaconExpirationPolicy
                                 callbacks =

        let toSoftwareVersion (ver: DefenderAvailability.Types.SoftwareVersion) =
            { major = int ver.Major; minor = int ver.Minor; buildNumber = int ver.Buildnumber }

        let toBeacon (pkt : Udp.UdpReceived) =
            try
                let av = DefenderAvailability.ParseFrom(pkt.udpResult.Buffer)
                let beacon =
                    { timestamp = pkt.timestamp
                      srcIpAddr = pkt.udpResult.RemoteEndPoint.Address
                      serialNumber = int av.SerialNumber
                      systemType = enum (int av.SystemType)
                      softwareVersion = toSoftwareVersion av.SoftwareVersion
                      connectionState = enum (int av.ConnectionState)
                      recordState = if av.HasRecordState then Some (enum (int av.RecordState)) else None
                      storageState = if av.HasStorageState then Some (enum (int av.StorageState)) else None
                      storageLevel = if av.HasStorageLevel then Some av.StorageLevel else None
                      batteryState = if av.HasBatteryState then Some (enum (int av.BatteryState)) else None
                      batteryLevel = if av.HasBatteryLevel then Some av.BatteryLevel else None }
                Some beacon
            with
                _ -> None

        let support = {
            new IBeaconSupport<DefenderBeacon, SerialNumber> with

                // Beacon management

                member __.GetKey beacon = beacon.serialNumber

                member __.IsChanged old newer = old.srcIpAddr <> newer.srcIpAddr

                // Logging

                member __.Added beacon =

                    defenderLog.DefenderDetected beacon.serialNumber beacon.srcIpAddr

                member __.Expired beacon lastUpdate =

                    defenderLog.DefenderExpired beacon.serialNumber lastUpdate beacon.srcIpAddr

                member __.Replaced old newer =

                    defenderLog.DefenderChangedAddress old.serialNumber old.srcIpAddr newer.srcIpAddr
        }

        let combinedNotifications = match callbacks with
                                    | Some callbacks -> combineNotifications support callbacks
                                    | None -> support

        new BeaconSource<DefenderBeacon, SerialNumber>(
                    beaconPort, expirationPeriod, toBeacon, combinedNotifications,
                    observationContext, beaconExpirationPolicy)

    /// Listens for and reports ARIS Defender beacons on the network.
    let mkCommandModuleBeaconListener beaconPort expirationPeriod observationContext beaconExpirationPolicy
                                      callbacks =

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

                    cmLog.CMDetected beacon.SrcIpAddr

                member __.Expired beacon lastUpdate =

                    cmLog.CMExpired lastUpdate beacon.SrcIpAddr

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
type AvailableDefenders = Beacons.BeaconSource<DefenderBeacon, SerialNumber>
type AvailableCommandModules = Beacons.BeaconSource<CommandModuleBeacon, int64>
