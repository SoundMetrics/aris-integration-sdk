// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open SoundMetrics.Aris.AcousticSettings
open System
open System.Net

// Very common types belong to the SoundMetrics.Common namespace.

/// Defines the serial number for an ARIS sonar. These are numeric only.
type ArisSerialNumber = uint32


type ArisAvailabilityState =
    | Available = 0
    | Busy = 1

/// Describes the onboard software version of an ARIS.
type ArisSoftwareVersion = { Major: int; Minor: int; BuildNumber: int }
with
    override x.ToString() = sprintf "%d.%d.%d" x.Major x.Minor x.BuildNumber

module internal ArisBeaconDetails =
    [<Literal>]
    let VoyagerVariant = "VG"

module Defender =
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

type DefenderState = {
    RecordState:    Defender.OnBoardRecordState
    StorageState:   Defender.OnBoardStorageState
    StorageLevel:   float32
    BatteryState:   Defender.OnBoardBatteryState
    BatteryLevel:   float32
}

type ArisModel = Explorer | Defender of DefenderState | Voyager

/// A beacon is sent to indicate the presence of an ARIS. ARIS Explorer and
/// ARIS Voyager send one beacon with the Model field indicating the type.
/// An ARIS Defender sends two beacons: one of model Explorer, and one of model
/// Defender. In a Defender's case, the beacon of model Explorer is always busy--
/// you cannot control the Defender directly.
type ArisBeacon = {
    Model:              ArisModel
    SystemType:         ArisSystemType
    SerialNumber:       ArisSerialNumber
    SoftwareVersion:    ArisSoftwareVersion
    Timestamp:          DateTime
    IPAddress:          IPAddress
    ConnectionState:    ArisAvailabilityState
    CpuTemp:            float32
}

/// A beacon is sent to indicate the presence of an ARIS. ARIS Explorer and
/// ARIS Voyager send one beacon with the Model field indicating the type.
/// An ARIS Defender sends two beacons: one of model Explorer, and one of model
/// Defender. In a Defender's case, the beacon of model Explorer is always busy--
/// you cannot control the Defender directly.
type ArisBeacon2 = {
    Model:              ArisModel
    SystemType:         ArisSystemType
    SerialNumber:       ArisSerialNumber
    SoftwareVersion:    ArisSoftwareVersion
    Timestamp:          DateTime
    SenderAddress:      IPAddress
    ConnectionState:    ArisAvailabilityState
    CpuTemp:            float32

    InterfaceInfo:      NetworkInterfaceInfo
}

type ArisCommandModuleBeacon = {
    IPAddress:      IPAddress
    ArisCurrent:    float32
    ArisPower:      float32
    ArisVoltage:    float32
    CpuTemp:        float32
    Revision:       uint32
    Timestamp:      DateTime
}

type ArisCommandModuleBeacon2 = {
    SenderAddress:  IPAddress
    ArisCurrent:    float32
    ArisPower:      float32
    ArisVoltage:    float32
    CpuTemp:        float32
    Revision:       uint32
    Timestamp:      DateTime

    InterfaceInfo:  NetworkInterfaceInfo
}


type NetworkDevice =
    | Aris of ArisBeacon
    | ArisCommandModule of ArisCommandModuleBeacon
