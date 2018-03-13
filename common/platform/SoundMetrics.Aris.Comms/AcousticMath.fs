// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SonarConfig
open System.Diagnostics

module AcousticMath =

    let private usPerS = 1000000.0

    let usToS (us: int<Us>): float<s> =
        (float (us / 1<Us>) / usPerS) * 1.0<s>

    let sToUs (s: float<s>): int<Us> =
        (int ((s / 1.0<s>) * usPerS)) * 1<Us>

    [<CompiledName("CalculateCyclePeriod")>]
    let calculateCyclePeriod systemType
                             (sampleStartDelay: int<Us>)
                             sampleCount
                             (samplePeriod: int<Us>)
                             (antiAliasing: int<Us>)
            : int<Us> =
        let ranges = systemTypeRangeMap.[systemType]
        let maxAllowedCyclePeriod = ranges.cyclePeriodRange.max
        let unboundedCyclePeriod = sampleStartDelay
                                    + (sampleCount * samplePeriod)
                                    + antiAliasing
                                    + cyclePeriodMargin
        min maxAllowedCyclePeriod unboundedCyclePeriod

    module private Native =
        open System.Runtime.InteropServices

        // NOTE: We'll need to start preloading the appropriate flavor of target
        // DLL if we begin to support x64.
        [<DllImport(@"Aris.Model.Native.dll", CallingConvention = CallingConvention.StdCall)>]
        extern double CalculateSpeedOfSound(double _temperatureC, double _depthM, double _salinityPPT);


    [<CompiledName("CalculateSpeedOfSound")>]
    let calculateSpeedOfSound (temperature: float<degC>) (depth: float<m>) (salinity: float) : float<m/s> =

        let T = temperature / 1.0<degC>
        let Z = depth / 1.0<m>
        let S = salinity

        // If you passed in NaN (etc.), you have failed me.
        let checkInput f name =
            if System.Double.IsNaN(f) then
                invalidArg name "is NaN"
            if System.Double.IsInfinity(f) then
                let flavor = if System.Double.IsPositiveInfinity(f) then
                                "+Infinity"
                             else
                                "-Infinity"
                invalidArg name ("is " + flavor)

        checkInput T "temperature"
        checkInput Z "depth"
        checkInput S "salinity"

        1.0<m/s> * Native.CalculateSpeedOfSound(T, Z, S)


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
        let pingsPerFrame = pingModeConfigurations.[pingMode].pingsPerFrame
        let cycleTimeUsec = (1.0<Us> * (float cyclePeriod) + cyclePeriodFactor) * float pingsPerFrame
        let rate = min (frameRateRange.max) (float32 (1000000.0<Us> / cycleTimeUsec) * 1.0f</s>)
        rate

    [<CompiledName("ConstrainAcousticSettings")>]
    let constrainAcousticSettings systemType (s: AcousticSettings) antiAliasing : AcousticSettings * bool =
        let maximumFrameRate = calculateMaximumFrameRate  systemType s.pingMode s.sampleStartDelay s.sampleCount s.samplePeriod antiAliasing
        let adjustedFrameRate = min s.frameRate maximumFrameRate

        let isConstrained = s.frameRate <> adjustedFrameRate
        let constrainedSettings = { s with frameRate = adjustedFrameRate }
        if isConstrained then Trace.TraceInformation("constrainAcousticSettings: constrained; " + (AcousticSettings.diff s constrainedSettings))
        constrainedSettings, isConstrained

