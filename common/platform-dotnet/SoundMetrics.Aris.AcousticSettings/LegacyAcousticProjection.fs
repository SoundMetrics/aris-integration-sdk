// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SoundMetrics.Aris.AcousticSettings.UnitsOfMeasure
open System

type LegacyFrameRate =
    | MaximumFrameRate
    | CustomFrameRate of FrameRate

type LegacyDetail =
    | AutoSamplePeriod
    | CustomSamplePeriod of int<Us>

type LegacySampleCount = {
    SampleCount : int
    IsFixed : bool
}

type LegacyTransmit = Off | LowPower | HighPower

type LegacyFrequency = LowFrequency | HighFrequency | AutoFrequency

type LegacyPulseWidth =
    | AutoPulseWidth
    | CustomPulseWidth of int<Us>
    | Narrow
    | Medium
    | Wide

/// `LegacyAcousticProjection` serves as the projection for `AcousticSettings`
/// as it was used in past work. While it largely just encloses `AcousticSettings`,
/// it is still a necessary and type-safe part of the new Projection-to-Settings
/// scheme. (This type is closer to the "view model" than the "model.")
type LegacyAcousticProjection = {
    FrameRate : LegacyFrameRate
    SampleCount : LegacySampleCount
    SampleStartDelay : int<Us>
    Detail : LegacyDetail // SamplePeriod
    PulseWidth : LegacyPulseWidth
    PingMode : PingMode
    Transmit : LegacyTransmit
    Frequency : LegacyFrequency
    ReceiverGain : int
}
with
    override s.ToString () = sprintf "%A" s

    static member Invalid = {
        FrameRate = CustomFrameRate 0.0</s>
        SampleCount = { SampleCount = 0; IsFixed = true }
        SampleStartDelay = 0<Us>
        Detail = CustomSamplePeriod 0<Us>
        PulseWidth = CustomPulseWidth 0<Us>
        PingMode = PingMode.InvalidPingMode 0
        Transmit = Off
        Frequency = LowFrequency
        ReceiverGain = 0
    }

type LegacyAcousticProjectionChange =
    | FrameRate of          LegacyFrameRate
    | SampleCount of        LegacySampleCount
    | SampleStartDelay of   int<Us>
    | Detail of             LegacyDetail // SamplePeriod
    | PulseWidth of         LegacyPulseWidth
    | PingMode of           PingMode
    | Transmit of           LegacyTransmit
    | Frequency of          LegacyFrequency
    | ReceiverGain of       int
    | All of                LegacyAcousticProjection

    // Higher-level actions

    | ResetDefaults
    | ChangeDownrangeStart of   float<m>
    | ChangeDownrangeEnd of     float<m>

    /// Translates the window up- or down-range while maintaining window size.
    | TranslateWindow of        downrangeStart: float<m>
    | Antialiasing of           int<Us>
