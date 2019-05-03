﻿// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.AcousticSettings.UnitsOfMeasure
open System

module internal LegacyAcousticProjectionDetails =
    open SoundMetrics.Aris.AcousticSettings
    open SoundMetrics.Aris.AcousticSettings.AcousticMath
    open SoundMetrics.Data
    open SoundMetrics.Data.Range


    //let deriveWindowLength (systemContext : SystemContext) (projection : LegacyAcousticProjection) =

    //    let t =

    //    internal static Distance CalculateWindowLength(FineDuration samplePeriod,
    //                                                   int samplesPerBeam,
    //                                                   Velocity speedOfSound)
    //    {
    //        // Halve the sample period to get actual resolution.
    //        FineDuration totalDuration = samplePeriod * samplesPerBeam;
    //        return (speedOfSound * totalDuration) / 2;
    //    }


    //let private deriveWindow systemContext projection : Range<float<m>> =

    //    failwith "nyi"


    let deriveMaxSamplePeriod (projection : LegacyAcousticProjection) =

        let asMicroseconds (f : float) = int f * 1<Us>

        let maxCP = SonarConfig.CyclePeriodRange.Max

        (float  (maxCP - projection.SampleStartDelay
                    - projection.AntialiasingPeriod - SonarConfig.CyclePeriodMargin)
            / float projection.SampleCount.SampleCount)
        |> Math.Floor //.Floor()
        |> asMicroseconds
        |> Range.constrainTo SonarConfig.SamplePeriodRange


    let deriveAutoSamplePeriod (projection : LegacyAcousticProjection) = failwith "nyi"

        //internal /* static */ FineDuration CalculateBestSamplePeriodForWindow(FineDuration currentSamplePeriod, Distance windowLength, int samplesPerBeam)
        //{
        //    FineDuration bestSamplePeriod = (windowLength / samplesPerBeam) / (SpeedOfSound / 2);
        //    return _acousticSettingsRanges.SamplePeriodRange.ConstrainValue(bestSamplePeriod);
        //}



    // Constrains the requested frame rate to fit the proposted projection.
    // Min frame rate is a constant, max is dynamic based on a number of factors
    // in the projection and other context.
    let private constrainRequestedFrameRate systemContext
                                            projection
                                            (requestedFrameRate : FrameRate)
                                            : FrameRate =

        let maxDynamiceRange =

            let maxDynamicFrameRate =
                let samplePeriod =
                    match projection.Detail with
                    | CustomSamplePeriod samplePeriod -> samplePeriod
                    | AutoSamplePeriod -> deriveAutoSamplePeriod projection

                calculateMaximumFrameRate systemContext.SystemType
                                          projection.PingMode
                                          projection.SampleStartDelay
                                          projection.SampleCount.SampleCount
                                          samplePeriod
                                          projection.AntialiasingPeriod

            range SonarConfig.FrameRateRange.Min maxDynamicFrameRate

        requestedFrameRate |> Range.constrainTo maxDynamiceRange


    let private deriveAutoFrequency systemContext
                                    (projection : LegacyAcousticProjection)
                                    : Frequency = // Return proper type for AcousticSettings raw frequency

        failwith "nyi"
        //let window = deriveWindow systemContext projection
        //let crossover =
        //    SonarConfig.systemTypeRangeMap.[systemContext.SystemType].UsableRange.LFCrossoverRange

        //if crossover < window.Max then
        //    Frequency.Low
        //else
        //    Frequency.High


    // Applies the requested frame rate to the given projection, constraining it if appropriate.
    let applyFrameRate systemContext
                       (projection : LegacyAcousticProjection)
                       (requestedFrameRate : LegacyFrameRate)
                       : LegacyAcousticProjection =

        let newFrameRate =
            match requestedFrameRate with
            | MaximumFrameRate -> MaximumFrameRate
            | CustomFrameRate rate ->
                let constrainedRate = constrainRequestedFrameRate systemContext projection rate
                CustomFrameRate constrainedRate

        { projection with FrameRate = newFrameRate }


    let applySampleCount systemContext
                         (projection : LegacyAcousticProjection) = failwith "nyi"


    let applySampleStartDelay systemContext
                              (projection : LegacyAcousticProjection) = failwith "nyi"


    let applyDetail systemContext
                    (projection : LegacyAcousticProjection) = failwith "nyi"


    let applyPulseWidth systemContext
                        (projection : LegacyAcousticProjection)
                        pulseWidth =

        failwith "nyi"

    let applyPingMode systemContext
                      (projection : LegacyAcousticProjection) = failwith "nyi"


    let applyFrequency systemContext
                       (projection : LegacyAcousticProjection)
                       frequency =

        if not (Enum.IsDefined(typeof<LegacyFrequency>, frequency)) then
            raise (invalidArg "frequency" "Unexpected frequency value")

        { projection with Frequency = frequency }


    /// Local version of Range.constrainTo that logs when a value is constrained.
    let constrainWithLog<'T when 'T : equality and 'T : comparison>
                    (range : Range<'T>)
                    name
                    (originalValue : 'T) : 'T =

        let constrainedValue = originalValue |> Range.constrainTo range
        if constrainedValue <> originalValue then
            Log.Information(
                "LegacyAcousticProjection: constrained {name}; was {originalValue}; now {constrainedValue}",
                name, originalValue, constrainedValue)

        constrainedValue


    //-------------------------------------------------------------------------
    // Top-level implementation of the functions necessary for mapping the project.

    let constrainProjection (_systemContext: SystemContext) (projection: LegacyAcousticProjection) =

        let frameRate =
            match projection.FrameRate with
            | MaximumFrameRate -> projection.FrameRate
            | CustomFrameRate fps ->
                CustomFrameRate (
                    ("FrameRate", fps) ||> constrainWithLog SonarConfig.FrameRateRange)

        let sampleCount =
            { projection.SampleCount
                with SampleCount =
                        ("SampleCount", projection.SampleCount.SampleCount) ||> constrainWithLog SonarConfig.SampleCountRange }

        let sampleStartDelay =
            ("SampleStartDelay", projection.SampleStartDelay) ||> constrainWithLog SonarConfig.SampleStartDelayRange

        let detail =
            match projection.Detail with
            | AutoSamplePeriod ->  projection.Detail
            | CustomSamplePeriod sp ->
                CustomSamplePeriod (
                    ("SamplePeriod", sp) ||> constrainWithLog SonarConfig.SamplePeriodRange)

        let pulseWidth =
            match projection.PulseWidth with
            | CustomPulseWidth pw ->
                CustomPulseWidth (
                    ("PulseWidth", pw) ||> constrainWithLog SonarConfig.PulseWidthRange)
            | AutoPulseWidth
            | Narrow
            | Medium
            | Wide -> projection.PulseWidth

        let receiverGain =
            ("ReceiverGain", projection.ReceiverGain) ||> constrainWithLog SonarConfig.ReceiverGainRange

        {
            projection with
                FrameRate = frameRate
                SampleCount = sampleCount
                SampleStartDelay = sampleStartDelay
                Detail = detail
                PulseWidth = pulseWidth
                ReceiverGain = receiverGain
        }

    let applyChange systemContext projection change : struct (LegacyAcousticProjection * AcousticSettings) =

        let newProjection =
            match change with
            | RequestFrameRate frameRate -> applyFrameRate systemContext projection frameRate

            | RequestSampleCount sampleCount -> failwith "nyi"
            | RequestSampleStartDelay ssd -> failwith "nyi"
            | RequestDownrangeStart rangeStart -> failwith "nyi"
            | RequestDownrangeEnd rangeEnd -> failwith "nyi"

            | RequestDetail detail -> failwith "nyi"
            | RequestPulseWidth pulseWidth -> applyPulseWidth systemContext projection pulseWidth
            | RequestPingMode pingMode -> failwith "nyi"

            | RequestTransmit transmit -> { projection with Transmit = transmit }

            | RequestFrequency frequency -> applyFrequency systemContext projection frequency

            | RequestReceiverGain gain -> { projection with ReceiverGain = gain |> Range.constrainTo SonarConfig.ReceiverGainRange }

            | RequestNewProjection newProjection -> newProjection

            // Higher-level actions

            | ResetDefaults -> failwith "nyi"

            /// Translates the window up- or down-range while maintaining window size.
            | RequestTranslateWindow downrangeStart -> failwith "nyi"
            | RequestAntialiasing antialiasing -> failwith "nyi"


        struct (newProjection, failwith "nyi")


    let toSettings systemContext (projection: LegacyAcousticProjection) : AcousticSettings =

        let frequency =
            match projection.Frequency with
            | LowFrequency -> Frequency.Low
            | HighFrequency -> Frequency.High
            | AutoFrequency -> deriveAutoFrequency systemContext projection

        let struct (enableTransmit, enable150Volts) =
            match projection.Transmit with
            | Off ->        struct (false, false)
            | LowPower ->   struct (true,  false)
            | HighPower ->  struct (true,  true)

        let samplePeriod =
            match projection.Detail with
            | CustomSamplePeriod sp -> sp
            | AutoSamplePeriod -> failwith "nyi"

        let cyclePeriod = failwith "nyi"
        let pulseWidth = failwith "nyi"

        let frameRate =
            let requested =
                match projection.FrameRate with
                | CustomFrameRate fr -> fr
                | MaximumFrameRate -> SonarConfig.FrameRateRange.Max
            constrainRequestedFrameRate systemContext projection requested

        {
            FrameRate           = frameRate
            SampleCount         = projection.SampleCount.SampleCount
            SampleStartDelay    = projection.SampleStartDelay
            CyclePeriod         = cyclePeriod
            SamplePeriod        = samplePeriod
            PulseWidth          = pulseWidth
            PingMode            = projection.PingMode
            EnableTransmit      = enableTransmit
            Frequency           = frequency
            Enable150Volts      = enable150Volts
            ReceiverGain        = projection.ReceiverGain
            AntialiasingPeriod  = projection.AntialiasingPeriod
        }
