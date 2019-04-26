// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings

[<AutoOpen>]
module UnitsOfMeasure =

    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

    [<Measure>] type degC
    [<Measure>] type mm
    [<Measure>] type Us // Microseconds

    /// Virtual focus unit, used to communicate focus position to ARIS.
    [<Measure>] type vfu

    let private usPerS = 1000000.0

    let usToS (us: int<Us>): float<s> =
        (float (us / 1<Us>) / usPerS) * 1.0<s>

    let sToUs (s: float<s>): int<Us> =
        (int ((s / 1.0<s>) * usPerS)) * 1<Us>

    let mToMm (m: float<m>): float<mm> =
        float m * 0.001<mm>
