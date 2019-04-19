// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open System

type DownrangeWindow = SoundMetrics.Aris.AcousticSettings.AcousticMath.DownrangeWindow

type BunnyHillSettings = {
    DownrangeWindow:    DownrangeWindow
}

type BunnyHillChange =
    | ChangeWindowStart of value: float<m>
    | ChangeWindowEnd of value: float<m>

module BunnyHill =

    [<CompiledName("BunnyHillProjection")>]
    let bunnyHillProjection =

        let toSettings externalContext (bunnyHill: BunnyHillSettings) : AcquisitionSettings =
            failwith "nyi"

        let change bunnyHill change : BunnyHillSettings =

            match change with
            | ChangeWindowStart value ->
                { bunnyHill with
                    DownrangeWindow =
                        { bunnyHill.DownrangeWindow with Start = value } }
            | ChangeWindowEnd value ->
                { bunnyHill with
                    DownrangeWindow =
                        { bunnyHill.DownrangeWindow with End = value } }

        let constrain (bunnyHill: BunnyHillSettings) (systemContext: SystemContext) =

            let struct (windowStartMin, windowEndMax) =
                let ranges = SoundMetrics.Aris.AcousticSettings.SonarConfig
                                .systemTypeRangeMap.[systemContext.SystemType]
                struct (ranges.WindowStartRange.Min, ranges.WindowEndRange.Max)

            let minmax f = max windowStartMin (min windowEndMax f)

            let struct (windowStart, windowEnd) =
                let struct (s, e) =
                    let downrange = bunnyHill.DownrangeWindow
                    struct (minmax downrange.Start, minmax downrange.End)
                struct (min s e, max s e)
            { DownrangeWindow = { Start = windowStart; End = windowEnd } }

        {
            ToAcquisitionSettings = Func<SystemContext,BunnyHillSettings,AcquisitionSettings>(toSettings)
            Constrain = Func<BunnyHillSettings,SystemContext,BunnyHillSettings>(constrain)
            Change = Func<BunnyHillSettings,BunnyHillChange,BunnyHillSettings>(change)
        }
