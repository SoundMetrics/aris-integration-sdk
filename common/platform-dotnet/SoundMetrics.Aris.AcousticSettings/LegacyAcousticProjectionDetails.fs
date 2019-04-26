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

    let deriveSamplePeriod projection = failwith "nyi"


    let constrainRequestedFrameRate systemContext
                                    projection
                                    (requestedFrameRate : FrameRate)
                                    : FrameRate =

            let maxDynamiceRange =

                let maxDynamicFrameRate =
                    let samplePeriod =
                        match projection.Detail with
                        | CustomSamplePeriod samplePeriod -> samplePeriod
                        | AutoSamplePeriod -> deriveSamplePeriod projection

                    calculateMaximumFrameRate systemContext.SystemType
                                              projection.PingMode
                                              projection.SampleStartDelay
                                              projection.SampleCount.SampleCount
                                              samplePeriod
                                              systemContext.AntialiasingPeriod
                range SonarConfig.FrameRateRange.Min maxDynamicFrameRate

            requestedFrameRate |> Range.constrainTo maxDynamiceRange


    let applyFrameRate systemContext
                       (projection : LegacyAcousticProjection)
                       (requestedFrameRate : LegacyFrameRate)
                       : LegacyAcousticProjection =

        match requestedFrameRate with
        | MaximumFrameRate -> { projection with FrameRate = MaximumFrameRate }
        | CustomFrameRate rate ->
            let constrainedRate = constrainRequestedFrameRate systemContext projection rate
            { projection with FrameRate = CustomFrameRate constrainedRate }


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
                       (projection : LegacyAcousticProjection) = failwith "nyi"


    //-------------------------------------------------------------------------
    // Top-level implementation of the functions necessary for mapping the project.

    let constrainProjection (systemContext: SystemContext) (projection: LegacyAcousticProjection) =

        failwith "nyi"

    let applyChange systemContext projection change : LegacyAcousticProjection =

        match change with
        | FrameRate frameRate -> applyFrameRate systemContext projection frameRate

        //| SampleCount sampleCount
        //| SampleStartDelay ssd
        //| Detail detail
        //| PulseWidth puseWidth
        //| PingMode pingMode

        | Transmit transmit -> { projection with Transmit = transmit }

        //| Frequency frequency

        | ReceiverGain gain -> { projection with ReceiverGain = gain |> Range.constrainTo SonarConfig.ReceiverGainRange }

        | All newProjection -> projection

    let toSettings systemContext (projection: LegacyAcousticProjection) : AcousticSettings =

        failwith "nyi"
