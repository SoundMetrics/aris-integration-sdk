// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SoundMetrics.Aris.AcousticSettings.UnitsOfMeasure
open System

module internal LegacyAcousticSettingsCore =

    ()

module internal LegacyAcousticSettingsDetails =
    open LegacyAcousticSettingsCore

    let constrainProjection (systemContext: SystemContext) (settings: LegacyAcousticSettings) =

        failwith "nyi"

    let applyChange systemContext settings change : LegacyAcousticSettings =

        failwith "nyi"

    let toSettings systemContext (settings: LegacyAcousticSettings) : AcousticSettings =

        failwith "nyi"
