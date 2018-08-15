// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open System
open System.Net

// Very common types belong to the SoundMetrics.Common namespace.

type ArisSerialNumber = uint32

type ArisSystemType = Aris1800 = 0 | Aris3000 = 1 | Aris1200 = 2


module ArisBeaconDetails =

    type ArisAvailabilityState =
        | Available = 0
        | Busy = 1

    /// Describes the onboard software version of an ARIS.
    type ArisSoftwareVersion = { Major: int; Minor: int; BuildNumber: int }
    with
        override x.ToString() = sprintf "%d.%d.%d" x.Major x.Minor x.BuildNumber

    [<Literal>]
    let internal VoyagerVariant = "VG"

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
        RecordState :   Defender.OnBoardRecordState
        StorageState :  Defender.OnBoardStorageState
        StorageLevel :  float32
        BatteryState :  Defender.OnBoardBatteryState
        BatteryLevel :  float32
    }

    type ArisModel = Explorer | Defender of DefenderState | Voyager

    type ArisBeacon = {
        Model :             ArisModel
        SystemType :        ArisSystemType
        SerialNumber :      ArisSerialNumber
        SoftwareVersion :   ArisSoftwareVersion
        Timestamp :         DateTime
        IPAddress :         IPAddress
        ConnectionState :   ArisAvailabilityState
        CpuTemp :           float32
    }

module ArisCommandModuleDetails =

    type ArisCommandModuleBeacon = {
        IPAddress :     IPAddress

        // sonar_serial_number[] is not supported
        ArisCurrent :   float32
        ArisPower :     float32
        ArisVoltage :   float32
        CpuTemp :       float32
        Revision :      uint32
        Timestamp :     DateTime
    }


open ArisBeaconDetails
open ArisCommandModuleDetails

type NetworkDevice =
    | Aris of ArisBeacon
    | ArisCommandModule of ArisCommandModuleBeacon
