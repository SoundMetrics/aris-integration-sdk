// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open SoundMetrics.Aris.Config
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open System
open System.Diagnostics

module private AcousticMathDetails =

        // Calculate the speed of sound based on temperature, depth and salinity, per
        // the associated documents.
        //
        // We're calculating to the first-order depth correction, nothing beyond that.
        //
        // C = 1402.5 + (5 * T) - (5.44e-2 * T*T) + (2.1e-4 * T*T*T) + 1.33*S -(1.23e-2 * S * T) + (8.7e-5 * S * T*T)	// first order for small depths
        // 
        // + (1.56e-2 * Z) + (2.55e-7 * Z*Z) -(7.3e-12 * Z*Z*Z)     // first order depth correction, max +4.70 m/s @ 300 m
        // + (1.2e-6 * Z * (Theta - 45))  -(9.5e-13 * T * Z*Z*Z)    // second order latitude/temperature/depth, max +/- .004 m/s @ 300m over latitude 0 to 90 degrees 
        // + (3e-7 * T*T * Z) + (1.43e-5 * S * Z)                   // third order temperature/salinity/depth, max + .29 m/s @ 40°C, 35ppt, 300m 

        let calculateSpeedOfSound(temperatureC : Double, depthM : Double, salinityPPT : Double) : Double =

            let T = temperatureC;
            let Z = depthM;
            let S = salinityPPT;

            1402.5 + (5.0 * T) - (5.44e-2 * T*T) + (2.1e-4 * T*T*T) + 1.33*S - (1.23e-2 * S * T) + (8.7e-5 * S * T*T) // first order for small depths
                + (1.56e-2 * Z) + (2.55e-7 * Z*Z) - (7.3e-12 * Z*Z*Z) // first order depth correction, max +4.70 m/s @ 300 m

        let validateDouble f name =

            if System.Double.IsNaN(f) then
                invalidArg name "is NaN"
            if System.Double.IsInfinity(f) then
                let flavor = if System.Double.IsPositiveInfinity(f) then
                                "+Infinity"
                             else
                                "-Infinity"
                invalidArg name ("is " + flavor)


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
        if isConstrained then Trace.TraceInformation("constrainAcousticSettings: constrained; " + (AcousticSettings.diff s constrainedSettings))
        constrainedSettings, isConstrained

