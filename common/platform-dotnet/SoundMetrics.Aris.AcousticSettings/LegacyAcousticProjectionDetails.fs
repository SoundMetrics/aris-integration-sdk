// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SoundMetrics.Aris.AcousticSettings.UnitsOfMeasure
open System

module internal LegacyAcousticProjectionDetails =
    open SoundMetrics.Aris.AcousticSettings
    open SoundMetrics.Aris.AcousticSettings.AcousticMath
    open SoundMetrics.Data
    open SoundMetrics.Data.Range

    let deriveAutoSamplePeriod projection = failwith "nyi"


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


    let private deriveWindow _systemContext _projection : Range<float<m>> =

        failwith "nyi"


    let private deriveAutoFrequency systemContext
                                    (projection : LegacyAcousticProjection)
                                    : Frequency = // Return proper type for AcousticSettings frequency

        let window = deriveWindow systemContext projection

        if window.Max > SonarConfig.systemTypeRangeMap.[systemContext.SystemType].UsableRange.LFCrossoverRange then
            Frequency.Low
        else
            Frequency.High


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
                        (projection : LegacyAcousticProjection) = failwith "nyi"


    let applyPingMode systemContext
                      (projection : LegacyAcousticProjection) = failwith "nyi"


    let applyFrequency systemContext
                       (projection : LegacyAcousticProjection)
                       frequency =

        if not (Enum.IsDefined(typeof<LegacyFrequency>, frequency)) then
            raise (invalidArg "frequency" "Unexpected frequency value")

        { projection with Frequency = frequency }


    //-------------------------------------------------------------------------
    // Top-level implementation of the functions necessary for mapping the project.

    let constrainProjection (systemContext: SystemContext) (projection: LegacyAcousticProjection) =

        failwith "nyi"

    let applyChange systemContext projection change : LegacyAcousticProjection =

        let newProjection =
            match change with
            | RequestFrameRate frameRate -> applyFrameRate systemContext projection frameRate

            | RequestSampleCount sampleCount -> failwith "nyi"
            | RequestSampleStartDelay ssd -> failwith "nyi"
            | RequestDownrangeStart rangeStart -> failwith "nyi"
            | RequestDownrangeEnd rangeEnd -> failwith "nyi"

            | RequestDetail detail -> failwith "nyi"
            | RequestPulseWidth puseWidth -> failwith "nyi"
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

        newProjection


    let toSettings systemContext (projection: LegacyAcousticProjection) : AcousticSettings =

        let _rawFrequency =
            match projection.Frequency with
            | LowFrequency -> Frequency.Low
            | HighFrequency -> Frequency.High
            | AutoFrequency -> deriveAutoFrequency systemContext projection

        failwith "nyi"
