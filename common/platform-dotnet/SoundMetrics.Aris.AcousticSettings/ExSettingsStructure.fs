// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open System
open SoundMetrics.Aris.AcousticSettings.AcousticMath
open SoundMetrics.Aris.AcousticSettings.UnitsOfMeasure

// Temporary aliases needed to use existing types without opening the
// entire namespace into this experimental namespace.
type PingMode = SoundMetrics.Aris.AcousticSettings.PingMode
type Frequency = SoundMetrics.Aris.AcousticSettings.Frequency
type Salinity = SoundMetrics.Aris.AcousticSettings.Salinity
type ArisSystemType = SoundMetrics.Aris.AcousticSettings.ArisSystemType
type FrameRate = SoundMetrics.Aris.AcousticSettings.FrameRate


// Formerly "AcousticSettings," these are the settings we send to the sonar to
// instruct it to form images.
type AcquisitionSettings = {
    FrameRate: FrameRate
    SampleCount: int
    SampleStartDelay: int<Us>
    CyclePeriod: int<Us>
    SamplePeriod: int<Us>
    PulseWidth: int<Us>
    PingMode: PingMode
    EnableTransmit: bool
    Frequency: Frequency
    Enable150Volts: bool
    ReceiverGain: int }
with
    override s.ToString () = sprintf "%A" s

    member s.ToShortString () =
        // Using String.Format in order to avoid the pain at the intersection of sprintf and unit of measure (F# 3.1.2).
        System.String.Format(
            "fr={0}; sc={1}; ssd={2}; cp={3}; sp={4}; pw={5}; pm={6}; tx={7}; freq={8}; 150v={9}; rcvgn={10}",
            s.FrameRate, s.SampleCount, s.SampleStartDelay, s.CyclePeriod, s.SamplePeriod,
            s.PulseWidth, s.PingMode, s.EnableTransmit, s.Frequency, s.Enable150Volts, s.ReceiverGain)

    static member Invalid = {
        FrameRate = 1.0</s>
        SampleCount = 0
        SampleStartDelay = 0<Us>
        CyclePeriod = 0<Us>
        SamplePeriod = 0<Us>
        PulseWidth = 0<Us>
        PingMode = PingMode.InvalidPingMode 0
        EnableTransmit = false
        Frequency = Frequency.Low
        Enable150Volts = false
        ReceiverGain = 0
    }

    static member diff left right =

        let addDiff name l r accum =

            if l <> r then
                let msg = sprintf "%s: %A => %A" name l r
                msg :: accum // reverse order
            else
                accum

        let differences =
            []
            |> addDiff "fr"        left.FrameRate          right.FrameRate
            |> addDiff "sc"        left.SampleCount        right.SampleCount
            |> addDiff "ssd"       left.SampleStartDelay   right.SampleStartDelay
            |> addDiff "cp"        left.CyclePeriod        right.CyclePeriod
            |> addDiff "sp"        left.SamplePeriod       right.SamplePeriod
            |> addDiff "pw"        left.PulseWidth         right.PulseWidth
            |> addDiff "pm"        left.PingMode           right.PingMode
            |> addDiff "tx"        left.EnableTransmit     right.EnableTransmit
            |> addDiff "freq"      left.Frequency          right.Frequency
            |> addDiff "150v"      left.Enable150Volts     right.Enable150Volts
            |> addDiff "recvgn"    left.ReceiverGain       right.ReceiverGain
            |> List.rev

        match differences with
        | [] -> ValueNone
        | ds -> ValueSome (System.String.Join("; ", ds))

    static member Diff(left, right) =
        match AcquisitionSettings.diff left right with
        | ValueSome s -> s
        | ValueNone -> ""

/// Supported lens types.
type AuxLensType = None | Telephoto

/// The context in which the system is operating.
type SystemContext = {
    SystemType: ArisSystemType
    WaterTemp:  float<degC>
    Salinity:   Salinity
    Depth:      float<m>
    AuxLens:    AuxLensType
    AntialiasingPeriod: int<Us>
}

/// Functions related in their effort to affect change, constraint, and conversion
/// to device settings for a particular projection. 'P is the projection type,
/// 'C is the change type. Using Func<> for interop with C#.
type ProjectionMap<'P,'C> = {
    /// Changes a projection instance; 'C is applied to 'P,
    /// producing a new 'P.
    Change:             Func<'P,SystemContext,'C,'P>

    /// Constrains settings projection 'P in ways that are specific to 'P.
    Constrain:          Func<'P,SystemContext,'P>

    /// Transforms from a projection of settings to actual device settings.
    ToAcquisitionSettings:   Func<SystemContext,'P,AcquisitionSettings>
}

//-----------------------------------------------------------------------------

/// These are computed from inputs given when projecting settings.
type ComputedValues = {
    Resolution:     float<mm>
    AutoFocusRange: float<m>
    SoundSpeed:     float<m/s>
    MaxFrameRate:   float</s>

    ActualDownrangeWindow: DownrangeWindow
    ConstrainedAcquisitionSettings: bool
}

module internal AcquisitionSettingsNormalization =

    /// Transforms to device settings that conform to guidelines for safely
    /// and successfully producing images.
    let normalize systemType
                  antialiasingPeriod
                  (settings: AcquisitionSettings)
                  : struct (AcquisitionSettings * bool) =

        let maximumFrameRate =
            calculateMaximumFrameRate systemType
                                      settings.PingMode
                                      settings.SampleStartDelay
                                      settings.SampleCount
                                      settings.SamplePeriod
                                      antialiasingPeriod
        let adjustedFrameRate = min settings.FrameRate maximumFrameRate

        let isConstrained = settings.FrameRate <> adjustedFrameRate
        let constrainedSettings = { settings with FrameRate = adjustedFrameRate }
        struct (constrainedSettings, isConstrained)

module ProjectionChange =

    open AcquisitionSettingsNormalization

    let private getComputedValues (systemContext: SystemContext)
                                  constrainedAS
                                  (acquisitionSettings: AcquisitionSettings)
                                  : ComputedValues =

        let sspd = calculateSpeedOfSound systemContext.WaterTemp
                                         systemContext.Depth
                                         systemContext.Salinity
        let window = calculateWindowAtSspd acquisitionSettings.SampleStartDelay
                                           acquisitionSettings.SamplePeriod
                                           acquisitionSettings.SampleCount
                                           sspd
        {
            Resolution = mToMm (window.Length / float acquisitionSettings.SampleCount)
            AutoFocusRange = window.MidPoint
            SoundSpeed = sspd
            MaxFrameRate =
                calculateMaximumFrameRate systemContext.SystemType
                                          acquisitionSettings.PingMode
                                          acquisitionSettings.SampleStartDelay
                                          acquisitionSettings.SampleCount
                                          acquisitionSettings.SamplePeriod
                                          systemContext.AntialiasingPeriod

            ActualDownrangeWindow = window
            ConstrainedAcquisitionSettings = constrainedAS
        }

    /// Applies changes to a settings projection; produces a new projection,
    /// device settings, and computed values.
    /// Intent: to provide useful projections of settings that users can
    /// more easily interact with, while also determining necessary device
    /// settings to produce appropriate images.
    [<CompiledName("ChangeProjection")>]
    let changeProjection<'P,'C> (pmap: ProjectionMap<'P,'C>)
                                (projection: 'P)
                                (changes: 'C seq)
                                (systemContext: SystemContext)
                                : struct ('P * AcquisitionSettings * ComputedValues) =

        // Unwrap the Funcs so we can fold, etc. (Func<> is used for ease of interop with C#.)
        let change projection change =
            pmap.Change.Invoke(projection, systemContext, change)
        let constrain systemContext projection =
            pmap.Constrain.Invoke(projection, systemContext)
        let toAcquisitionSettings ctx projection : AcquisitionSettings =
            pmap.ToAcquisitionSettings.Invoke(ctx, projection)

        let constrainedProjection =
            let projectionWithChanges = changes |> Seq.fold change projection
            projectionWithChanges |> constrain systemContext
        let struct (acquisitionSettings, constrainedAS) =
            toAcquisitionSettings systemContext constrainedProjection
                |> normalize systemContext.SystemType systemContext.AntialiasingPeriod
        let computedValues = acquisitionSettings
                                |> getComputedValues systemContext constrainedAS
        struct (constrainedProjection, acquisitionSettings, computedValues)

type ProjectionMap<'P,'C> with
    /// See ProjectionChange.changeProjection.
    member map.Apply(projection, changes, systemContext) =
        ProjectionChange.changeProjection map projection changes systemContext
