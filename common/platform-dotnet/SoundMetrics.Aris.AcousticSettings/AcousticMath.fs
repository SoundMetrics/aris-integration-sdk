// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SonarConfig
open SoundMetrics.Data
open SoundMetrics.Data.Range
open System

type AcousticSettingsRaw = {
    FrameRate:          FrameRate
    SampleCount:        int
    SampleStartDelay:   int<Us>
    CyclePeriod:        int<Us>
    SamplePeriod:       int<Us>
    PulseWidth:         int<Us>
    PingMode:           PingMode
    EnableTransmit:     bool
    Frequency:          Frequency
    Enable150Volts:     bool
    ReceiverGain:       int
}
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
        PingMode = InvalidPingMode 0
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

module AcousticMath =

    [<CompiledName("CalculateCyclePeriod")>]
    let calculateCyclePeriod systemType
                             (sampleStartDelay: int<Us>)
                             (sampleCount : int)
                             (samplePeriod: int<Us>)
                             (antiAliasing: int<Us>)
            : int<Us> =
        let ranges = SonarConfig.systemTypeRangeMap.[systemType]
        let maxAllowedCyclePeriod = ranges.CyclePeriodRange.Max
        let unboundedCyclePeriod = sampleStartDelay
                                    + (int sampleCount * samplePeriod)
                                    + antiAliasing
                                    + SonarConfig.CyclePeriodMargin
        min maxAllowedCyclePeriod unboundedCyclePeriod


    [<CompiledName("CalculateSpeedOfSound")>]
    let calculateSpeedOfSound (temperature: float<degC>)
                              (depth: float<m>)
                              (salinity: Salinity)
                              : SoundSpeed =

        AcousticMathDetails.validateDouble (float temperature)  "temperature"
        AcousticMathDetails.validateDouble (float depth)        "depth"
        AcousticMathDetails.validateDouble (float salinity)     "salinity"

        1.0<m/s> * AcousticMathDetails.calculateSpeedOfSound(temperature, depth, salinity)


    type DownrangeWindow = {
        Start:  float<m>
        End:    float<m>
    }
    with
        member x.Length = x.End - x.Start
        member x.MidPoint = x.Start + (x.Length / 2.0)
        override x.ToString() = sprintf "{windowStart=%f; windowEnd=%f}" (float x.Start) (float x.End)

    [<CompiledName("CalculateWindowAtSspd")>]
    let calculateWindowAtSspd (sampleStartDelay: int<Us>)
                              (samplePeriod: int<Us>)
                              (sampleCount: int)
                              (sspd : SoundSpeed)
                              : DownrangeWindow =
        let sampleStartDelay = usToS sampleStartDelay
        let samplePeriod = usToS samplePeriod
        let windowStart = sampleStartDelay * sspd / 2.0
        let windowLength = float sampleCount * samplePeriod * sspd / 2.0
        { Start = windowStart; End = windowStart + windowLength }

    [<CompiledName("CalculateWindow")>]
    let calculateWindow (sampleStartDelay: int<Us>)
                        (samplePeriod: int<Us>)
                        (sampleCount: int)
                        (temperature: float<degC>)
                        (depth: float<m>)
                        (salinity: Salinity)
                        : DownrangeWindow =
        let sspd = calculateSpeedOfSound temperature depth salinity
        calculateWindowAtSspd sampleStartDelay
                              samplePeriod
                              sampleCount
                              sspd

    [<CompiledName("CalculateSampleStartDelay")>]
    let calculateSampleStartDelay (windowStart: float<m>)
                                  (temperature: float<degC>)
                                  (depth: float<m>)
                                  (salinity: Salinity)
                                  : int<Us> =
        let sspd = calculateSpeedOfSound temperature depth salinity
        let ssd = 2.0 * windowStart / sspd
        sToUs ssd


    [<CompiledName("CalculateMaximumFrameRate")>]
    let calculateMaximumFrameRate systemType
                                  pingMode
                                  sampleStartDelay
                                  sampleCount
                                  samplePeriod
                                  antiAliasing
                                    : float</s> =

        // The cyclePeriodFactor is an empirical value based on measured maximum frame rates as a function of
        // SamplePeriod and SamplesPerBeam.

        // From measured achievable frame rates, there is a rough break in the slope of frame rate versus
        // SamplesPerBeam at SamplesPerBeam == 2000.  Use a higher (by BoostFactor = 2) slope when
        // SamplesPerBeam is greater than 2000, and a lower slope for SamplesPerBeam < 2000

        let cyclePeriodFactor = let minFactor = 1400.0<Us>
                                let samplesPerBeamThreshold = 2000
                                if sampleCount > samplesPerBeamThreshold then
                                    let boostFactor = 2.0
                                    minFactor + boostFactor * minFactor
                                        * (float)(sampleCount - samplesPerBeamThreshold)
                                        / (float)samplesPerBeamThreshold
                                else
                                    minFactor * (float)sampleCount / (float)samplesPerBeamThreshold

        // Add some additional delay at SamplePeriod == [4, 5, 6] based on SamplePeriod value
        // This accounts for observed performance loss at these sample periods

        let fastSamplePeriodLimit = 7<Us>
        let cyclePeriodFactor = cyclePeriodFactor +
                                    if samplePeriod < fastSamplePeriodLimit then
                                        let fastSamplePeriodFactor = 3000.0<Us>
                                        fastSamplePeriodFactor / float samplePeriod
                                    else
                                        0.0<Us>

        // Add more delay at shortest SamplePeriod == 4 to constrain to achievable frame rates

        let fastestSamplePeriodLimit = 4<Us>
        let cyclePeriodFactor = cyclePeriodFactor +
                                    if samplePeriod = fastestSamplePeriodLimit then
                                        let fastestSamplePeriodFactor = 400.0<Us>
                                        fastestSamplePeriodFactor
                                    else
                                        0.0<Us>

        let cyclePeriod = calculateCyclePeriod systemType sampleStartDelay sampleCount samplePeriod antiAliasing
        let pingsPerFrame = SonarConfig.pingModeConfigurations.[pingMode].PingsPerFrame
        let cycleTimeUsec = (1.0<Us> * (float cyclePeriod) + cyclePeriodFactor) * float pingsPerFrame
        let rate = min (SonarConfig.FrameRateRange.Max) ((1000000.0<Us> / cycleTimeUsec) * 1.0</s>)
        rate


    [<CompiledName("CalculateSampleStartDelayRange")>]
    let calculateSampleStartDelayRange systemType
                                  (sampleCount : int)
                                  (samplePeriod : int<Us>)
                                  (antialiasing : int<Us>)
                                  : Range<int<Us>> =

        let ranges = SonarConfig.systemTypeRangeMap.[systemType]

        let minSSD = ranges.SampleStartDelayRange.Min
        let maxSSD =
            let maxCP = ranges.CyclePeriodRange.Max
            maxCP - (sampleCount * samplePeriod + antialiasing + SonarConfig.CyclePeriodMargin)
                |> constrainTo ranges.SampleStartDelayRange

        range minSSD maxSSD

    [<CompiledName("CalculateSamplePeriodRange")>]
    let calculateSamplePeriodRange systemType
                                   (sampleCount : int)
                                   (sampleStartDelay : int<Us>)
                                   (antialiasing : int<Us>)
                                   : Range<int<Us>> =

        let ranges = SonarConfig.systemTypeRangeMap.[systemType]

        let minSP = ranges.SamplePeriodRange.Min
        let maxSP =
            let maxCP = ranges.CyclePeriodRange.Max

            let x =
                (float (maxCP - sampleStartDelay - antialiasing - SonarConfig.CyclePeriodMargin) / float sampleCount)
            (int (x |> Math.Floor)) * 1<Us>
                |> Range.constrainTo ranges.SamplePeriodRange

        range minSP maxSP

    [<CompiledName("CalculateSampleCountRange")>]
    let calculateSampleCountRange systemType
                                  (samplePeriod : int<Us>)
                                  (sampleStartDelay : int<Us>)
                                  (antialiasing : int<Us>)
                                  : Range<int> =

        let ranges = SonarConfig.systemTypeRangeMap.[systemType]

        let minSC = SonarConfig.SampleCountRange.Min
        let maxSC =
            let maxCP = ranges.CyclePeriodRange.Max
            (float (maxCP - sampleStartDelay - antialiasing - SonarConfig.CyclePeriodMargin) / float samplePeriod)
                |> Math.Floor |> int

        range minSC maxSC

    [<CompiledName("CalculateAntialiasingRange")>]
    let calculateAntialiasingRange systemType
                                   (sampleCount : int)
                                   (samplePeriod : int<Us>)
                                   (sampleStartDelay : int<Us>)
                                   : Range<int<Us>> =

        let ranges = SonarConfig.systemTypeRangeMap.[systemType]

        let minAA = SonarConfig.MinAntialiasing
        let maxAA =
            let maxCP = ranges.CyclePeriodRange.Max

            let cyclePeriodWithoutAntialiasing =
                calculateCyclePeriod systemType
                                     sampleStartDelay
                                     sampleCount
                                     samplePeriod
                                     SonarConfig.MinAntialiasing
            maxCP - cyclePeriodWithoutAntialiasing
                |> Range.constrainTo ranges.CyclePeriodRange

        range minAA maxAA

    [<CompiledName("FindAntialiasing")>]
    let FindAntialiasing (sampleCount : int)
                         (cyclePeriod : int<Us>)
                         (sampleStartDelay : int<Us>)
                         (samplePeriod : int<Us>)
                         : int<Us> =

        let value =
            (cyclePeriod
                - (sampleStartDelay
                    + (sampleCount * samplePeriod)
                    + SonarConfig.CyclePeriodMargin))
        max value SonarConfig.MinAntialiasing

    [<CompiledName("ConstrainAcousticSettings")>]
    let constrainAcousticSettings systemType
                                  (s: AcousticSettingsRaw)
                                  antiAliasing
                                  : struct (AcousticSettingsRaw * bool) =
        let maximumFrameRate =
            calculateMaximumFrameRate  systemType s.PingMode s.SampleStartDelay s.SampleCount s.SamplePeriod antiAliasing
        let adjustedFrameRate = min s.FrameRate maximumFrameRate

        let isConstrained = s.FrameRate <> adjustedFrameRate
        let constrainedSettings = { s with FrameRate = adjustedFrameRate }
        if isConstrained then Log.Information("constrainAcousticSettings: constrained; {settings}", (AcousticSettingsRaw.diff s constrainedSettings))
        struct (constrainedSettings, isConstrained)


module internal SettingsDetails =

    let defaultSettingsMap =
        [ ArisSystemType.Aris1200,
                               { FrameRate =        FrameRateRange.Min
                                 SampleCount =      1000
                                 SampleStartDelay = 4000<Us>
                                 CyclePeriod =      32400<Us>
                                 SamplePeriod =     28<Us>
                                 PulseWidth =       24<Us>
                                 PingMode = getDefaultPingModeForSystemType ArisSystemType.Aris1200
                                 EnableTransmit =   true
                                 Frequency =        Frequency.High
                                 Enable150Volts =   true
                                 ReceiverGain =     20 }

          ArisSystemType.Aris1800,
                               { FrameRate =        FrameRateRange.Min
                                 SampleCount =      1000
                                 SampleStartDelay = 2000<Us>
                                 CyclePeriod =      19400<Us>
                                 SamplePeriod =     17<Us>
                                 PulseWidth =       14<Us>
                                 PingMode = getDefaultPingModeForSystemType ArisSystemType.Aris1800
                                 EnableTransmit =   true
                                 Frequency =        Frequency.High
                                 Enable150Volts =   true
                                 ReceiverGain =     18 }

          ArisSystemType.Aris3000,
                               { FrameRate =        FrameRateRange.Min
                                 SampleCount =      1000
                                 SampleStartDelay = 1300<Us>
                                 CyclePeriod =      6700<Us>
                                 SamplePeriod =     5<Us>
                                 PulseWidth =       5<Us>
                                 PingMode = getDefaultPingModeForSystemType ArisSystemType.Aris3000
                                 EnableTransmit =   true
                                 Frequency =        Frequency.High
                                 Enable150Volts =   true
                                 ReceiverGain =     12 } ]
        |> Map.ofList

type AcousticSettingsRaw with
    static member DefaultAcousticSettingsFor systemType = SettingsDetails.defaultSettingsMap.[systemType]

    member internal s.Validate () =
        let pingsPerFrame = pingModeConfigurations.[s.PingMode].PingsPerFrame
        let framePeriod = int (ceil (1000000.0 / float s.FrameRate)) * 1<Us>
        let adjustedCyclePeriod = s.SampleStartDelay + (s.SamplePeriod * int s.SampleCount) + 360<Us>

        let NoError = ""
        let rawResults =
            [
                (if SampleCountRange |> contains s.SampleCount
                    then NoError
                    else sprintf "sampleCount '%d' is out-of-range" s.SampleCount)
                (if SampleCountRange |> contains s.SampleCount
                    then NoError
                    else sprintf "sampleCount '%d' is out-of-range" s.SampleCount)
                (if SampleStartDelayRange |> contains s.SampleStartDelay
                    then NoError
                    else sprintf "sampleStartDelay '%d' is out-of-range" (int s.SampleStartDelay))
                (if CyclePeriodRange |> contains s.CyclePeriod
                    then NoError
                    else sprintf "cyclePeriod '%d' is out-of-range" (int s.CyclePeriod))
                (if SamplePeriodRange |> contains s.SamplePeriod
                    then NoError
                    else sprintf "samplePeriod '%d' is out-of-range" (int s.SamplePeriod))
                (if PulseWidthRange |> contains s.PulseWidth
                    then NoError
                    else sprintf "pulseWidth '%d' is out-of-range" (int s.PulseWidth))
                (if ReceiverGainRange |> contains s.ReceiverGain
                    then NoError
                    else sprintf "receiverGain '%d' is out-of-range" s.ReceiverGain)
                (if framePeriod > s.CyclePeriod * int pingsPerFrame
                    then NoError
                    else sprintf "framePeriod '%d' is too small for cycle period; frame rate %f is too fast"
                                 (int framePeriod) (float s.FrameRate))
                (if s.CyclePeriod >= adjustedCyclePeriod
                    then NoError
                    else sprintf "cyclePeriod '%d' < %d" (int s.CyclePeriod) (int adjustedCyclePeriod))
            ]

        let problems = rawResults |> List.filter (fun p -> p <> NoError)
        problems
