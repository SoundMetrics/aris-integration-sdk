﻿// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SonarConfig
open SoundMetrics.Data.Range


type Salinity =
    | Fresh = 0
    | Brackish = 15
    | Seawater = 35

type AcousticSettingsRaw = {
    FrameRate: float32</s>
    SampleCount: uint32
    SampleStartDelay: int<Us>
    CyclePeriod: int<Us>
    SamplePeriod: int<Us>
    PulseWidth: int<Us>
    PingMode: PingMode
    EnableTransmit: bool
    Frequency: Frequency
    Enable150Volts: bool
    ReceiverGain: float32 }
with
    override s.ToString () = sprintf "%A" s

    member s.ToShortString () =
        // Using String.Format in order to avoid the pain at the intersection of sprintf and unit of measure (F# 3.1.2).
        System.String.Format(
            "fr={0}; sc={1}; ssd={2}; cp={3}; sp={4}; pw={5}; pm={6}; tx={7}; freq={8}; 150v={9}; rcvgn={10}",
            s.FrameRate, s.SampleCount, s.SampleStartDelay, s.CyclePeriod, s.SamplePeriod,
            s.PulseWidth, s.PingMode, s.EnableTransmit, s.Frequency, s.Enable150Volts, s.ReceiverGain)

    static member Invalid = {
        FrameRate = 1.0f</s>
        SampleCount = 0u
        SampleStartDelay = 0<Us>
        CyclePeriod = 0<Us>
        SamplePeriod = 0<Us>
        PulseWidth = 0<Us>
        PingMode = InvalidPingMode 0u
        EnableTransmit = false
        Frequency = Frequency.Low
        Enable150Volts = false
        ReceiverGain = 0.0f
    }

    static member diff left right =
        let buf = System.Text.StringBuilder()
        let count = ref 0
        let addDiff (name: string) l r =
            if l <> r then
                buf.AppendFormat("{0}{1}: {2} => {3}", (if !count > 0 then "; " else ""), name, l, r) |> ignore
                count := !count + 1

        addDiff "fr"     left.FrameRate         right.FrameRate
        addDiff "sc"     left.SampleCount       right.SampleCount
        addDiff "ssd"    left.SampleStartDelay  right.SampleStartDelay
        addDiff "cp"     left.CyclePeriod       right.CyclePeriod
        addDiff "sp"     left.SamplePeriod      right.SamplePeriod
        addDiff "pw"     left.PulseWidth        right.PulseWidth
        addDiff "pm"     left.PingMode          right.PingMode
        addDiff "tx"     left.EnableTransmit    right.EnableTransmit
        addDiff "freq"   left.Frequency         right.Frequency
        addDiff "150v"   left.Enable150Volts    right.Enable150Volts
        addDiff "recvgn" left.ReceiverGain      right.ReceiverGain

        buf.ToString()

module AcousticMath =

    let private usPerS = 1000000.0

    let usToS (us: int<Us>): float<s> =
        (float (us / 1<Us>) / usPerS) * 1.0<s>

    let sToUs (s: float<s>): int<Us> =
        (int ((s / 1.0<s>) * usPerS)) * 1<Us>

    [<CompiledName("CalculateCyclePeriod")>]
    let calculateCyclePeriod systemType
                             (sampleStartDelay: int<Us>)
                             (sampleCount : uint32)
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
    let calculateSpeedOfSound (temperature: float) (depth: float) (salinity: float) : float<m/s> =

        AcousticMathDetails.validateDouble temperature  "temperature"
        AcousticMathDetails.validateDouble depth        "depth"
        AcousticMathDetails.validateDouble salinity     "salinity"

        1.0<m/s> * AcousticMathDetails.calculateSpeedOfSound(temperature, depth, salinity)


    type WindowSize = {
        windowStart: float<m>
        windowLength: float<m>
    }
    with
        member x.midPoint = x.windowStart + (x.windowLength / 2.0)
        member x.windowEnd = x.windowStart + x.windowLength
        override x.ToString() = sprintf "{windowStart=%f; windowEnd=%f}" (float x.windowStart) (float x.windowEnd)

    [<CompiledName("CalculateWindow")>]
    let calculateWindow (sampleStartDelay: int<Us>)
                        (samplePeriod: int<Us>)
                        (sampleCount: int)
                        temperature
                        depth
                        salinity =
        let sampleStartDelay = usToS sampleStartDelay
        let samplePeriod = usToS samplePeriod
        let sspd = calculateSpeedOfSound temperature depth salinity
        let windowStart = sampleStartDelay * sspd / 2.0
        let windowLength = float sampleCount * samplePeriod * sspd / 2.0
        { windowStart = windowStart; windowLength = windowLength }

    [<CompiledName("CalculateSampleStartDelay")>]
    let calculateSampleStartDelay (windowStart: float<m>)
                                  temperature
                                  depth
                                  salinity : int<Us> =
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
                                    : float32</s> =

        // The cyclePeriodFactor is an empirical value based on measured maximum frame rates as a function of
        // SamplePeriod and SamplesPerBeam.

        // From measured achievable frame rates, there is a rough break in the slope of frame rate versus
        // SamplesPerBeam at SamplesPerBeam == 2000.  Use a higher (by BoostFactor = 2) slope when
        // SamplesPerBeam is greater than 2000, and a lower slope for SamplesPerBeam < 2000

        let cyclePeriodFactor = let minFactor = 1400.0<Us>
                                let samplesPerBeamThreshold = 2000u
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
        let rate = min (SonarConfig.FrameRateRange.Max) (float32 (1000000.0<Us> / cycleTimeUsec) * 1.0f</s>)
        rate

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
                               { FrameRate =        1.0f</s>
                                 SampleCount =      1000u
                                 SampleStartDelay = 4000<Us>
                                 CyclePeriod =      32400<Us>
                                 SamplePeriod =     28<Us>
                                 PulseWidth =       24<Us>
                                 PingMode = getDefaultPingModeForSystemType ArisSystemType.Aris1200
                                 EnableTransmit =   true
                                 Frequency =        Frequency.High
                                 Enable150Volts =   true
                                 ReceiverGain =     20.0f }

          ArisSystemType.Aris1800,
                               { FrameRate =        1.0f</s>
                                 SampleCount =      1000u
                                 SampleStartDelay = 2000<Us>
                                 CyclePeriod =      19400<Us>
                                 SamplePeriod =     17<Us>
                                 PulseWidth =       14<Us>
                                 PingMode = getDefaultPingModeForSystemType ArisSystemType.Aris1800
                                 EnableTransmit =   true
                                 Frequency =        Frequency.High
                                 Enable150Volts =   true
                                 ReceiverGain =     18.0f }

          ArisSystemType.Aris3000,
                               { FrameRate =        1.0f</s>
                                 SampleCount =      1000u
                                 SampleStartDelay = 1300<Us>
                                 CyclePeriod =      6700<Us>
                                 SamplePeriod =     5<Us>
                                 PulseWidth =       5<Us>
                                 PingMode = getDefaultPingModeForSystemType ArisSystemType.Aris3000
                                 EnableTransmit =   true
                                 Frequency =        Frequency.High
                                 Enable150Volts =   true
                                 ReceiverGain =     12.0f } ]
        |> Map.ofList

type AcousticSettingsRaw with
    static member DefaultAcousticSettingsFor systemType = SettingsDetails.defaultSettingsMap.[systemType]

    member internal s.Validate () =
        let pingsPerFrame = pingModeConfigurations.[s.PingMode].PingsPerFrame
        let framePeriod = int (ceil (1000000.0f / float32 s.FrameRate)) * 1<Us>
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
                (if ReceiverGainRange |> contains (uint32 s.ReceiverGain)
                    then NoError
                    else sprintf "receiverGain '%f' is out-of-range" s.ReceiverGain)
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
