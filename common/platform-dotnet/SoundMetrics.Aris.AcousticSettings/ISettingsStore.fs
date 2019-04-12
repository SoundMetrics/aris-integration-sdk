// Copyright 2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Store

type ArisCollectiveSettings =

    member __.A = 42


type IArisSettingsStore =
    abstract AddOrUpdate : key: uint32 -> bool
    abstract Remove : key: uint32 -> bool
