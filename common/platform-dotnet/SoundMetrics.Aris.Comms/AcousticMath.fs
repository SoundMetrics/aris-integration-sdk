// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.Comms.Internal
open SoundMetrics.Aris.Config

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
    let calculateSpeedOfSound (temperature: float<degC>) (depth: float<m>) (salinity: float) : float<m/s> =

        let T = temperature / 1.0<degC>
        let Z = depth / 1.0<m>
        let S = salinity

        AcousticMathDetails.validateDouble T "temperature"
        AcousticMathDetails.validateDouble Z "depth"
        AcousticMathDetails.validateDouble S "salinity"

        1.0<m/s> * AcousticMathDetails.calculateSpeedOfSound(T, Z, S)


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
        // Valid for FPGA code in CPU2 as of March 2014...maximum frame rates may increase when FPGA data 
        // transfer time is reduced by cutting wait states on the CPU<-->FPGA<-->Sample RAM bus.
        // Set cyclePeriodFactor to 0 for pure acoustic frame rate limit (for testing only)

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
    let constrainAcousticSettings systemType (s: AcousticSettings) antiAliasing : AcousticSettings * bool =
        let maximumFrameRate =
            calculateMaximumFrameRate  systemType s.PingMode s.SampleStartDelay s.SampleCount s.SamplePeriod antiAliasing
        let adjustedFrameRate = min s.FrameRate maximumFrameRate

        let isConstrained = s.FrameRate <> adjustedFrameRate
        let constrainedSettings = { s with FrameRate = adjustedFrameRate }
        if isConstrained then Log.Information("constrainAcousticSettings: constrained; {settings}", (AcousticSettings.diff s constrainedSettings))
        constrainedSettings, isConstrained

