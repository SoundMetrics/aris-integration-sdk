// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SoundMetrics.Aris.AcousticSettings.UnitsOfMeasure
open System

module internal LegacyAcousticProjectionCore =

    ()

module internal LegacyAcousticProjectionDetails =
    open LegacyAcousticProjectionCore

    let constrainProjection (systemContext: SystemContext) (projection: LegacyAcousticProjection) =

        failwith "nyi"

    let applyChange systemContext projection change : LegacyAcousticProjection =

        failwith "nyi"

    let toSettings systemContext (projection: LegacyAcousticProjection) : AcousticSettings =

        failwith "nyi"
