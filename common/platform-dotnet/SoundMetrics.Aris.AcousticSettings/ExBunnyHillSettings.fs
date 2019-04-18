// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

type Range = float32

type BunnyHillSettings = {
    WindowStart:    Range
    WindowEnd:      Range
}

type BunnyHillChange =
    | ChangeWindowStart of value: Range
    | ChangeWindowEnd of value: Range

module BunnyHill =

    [<CompiledName("BunnyHillProjection")>]
    let bunnyHillProjection =

        let toSettings externalContext (bunnyHill : BunnyHillSettings) =
            { PingMode = 12 }

        let change bunnyHill change : BunnyHillSettings =

            match change with
            | ChangeWindowStart value ->
                { bunnyHill with WindowStart = value }
            | ChangeWindowEnd value ->
                { bunnyHill with WindowEnd = value }

        let minWindowStart = 0.7f
        let maxWindowEnd = 20.0f

        let constrain (bunnyHill : BunnyHillSettings) =
            let minmax f = max minWindowStart (min maxWindowEnd f)
            let struct (windowStart, windowEnd) =
                let struct (s, e) =
                    struct (minmax bunnyHill.WindowStart, minmax bunnyHill.WindowEnd)
                struct (min s e, max s e)
            { WindowStart = windowStart; WindowEnd = windowEnd }

        { ToSettings = toSettings
          Constrain = constrain
          Change = change }
