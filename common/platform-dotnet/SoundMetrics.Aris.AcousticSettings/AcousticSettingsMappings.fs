// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

module AcousticSettingsMappings =

    open LegacyAcousticSettingsDetails

    [<CompiledName("LegacyAcousticSettingsMapping")>]
    let legacyAcousticSettingsMapping =

        {
            new IProjectionMap<LegacyAcousticSettings,LegacyAcousticSettingsChange> with

                member __.ConstrainProjection systemContext projection =
                    constrainProjection systemContext projection

                member __.ApplyChange systemContext projection changeRequest =
                    applyChange systemContext projection changeRequest

                member __.ToAcquisitionSettings systemContext projection =
                    toSettings systemContext projection
        }
