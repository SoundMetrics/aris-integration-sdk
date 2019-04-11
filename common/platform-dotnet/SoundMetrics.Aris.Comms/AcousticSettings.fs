// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open SoundMetrics.Aris.AcousticSettings


/// Represents a versioned AcoustincSettings; a cookie is used to uniquely identify
/// a specific AcousticSettings so the protocols can refer to settings commands.
type internal AcousticSettingsVersioned = {
    Cookie : AcousticSettingsCookie
    Settings: AcousticSettingsRaw
}
with
    static member InvalidAcousticSettingsCookie = 0u

    static member Invalid = {
        Cookie = AcousticSettingsVersioned.InvalidAcousticSettingsCookie
        Settings = AcousticSettingsRaw.Invalid
    }

/// Used to track the latest acoustic settings applied.
type internal AcousticSettingsApplied =
    | Uninitialized
    | Applied of AcousticSettingsVersioned
    | Constrained of AcousticSettingsVersioned
    | Invalid of cookie: AcousticSettingsCookie * settings: AcousticSettingsRaw
