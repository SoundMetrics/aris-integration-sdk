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

        let validateWindow (downrange : DownrangeWindow) =

            if Double.IsNaN(float downrange.Start) || Double.IsInfinity(float downrange.Start) then
                invalidArg "Start" "Is NaN or infinity"
            if downrange.Start <= 0.0<m> then
                invalidArg "Start" "Is less than or equal to zero"

            if Double.IsNaN(float downrange.End) || Double.IsInfinity(float downrange.End) then
                invalidArg "End" "Is NaN or infinity"
            if downrange.End <= 0.0<m> then
                invalidArg "End" "Is less than or equal to zero"

            if downrange.Start >= downrange.End then
                invalidArg "Start" "Is greater or equal to End"


        let toSettings (bunnyHill: BunnyHillSettings) externalContext : AcousticSettings =

            validateWindow bunnyHill.DownrangeWindow
            AcousticSettings.Invalid

        let change bunnyHill systemContext change : BunnyHillSettings =

            validateWindow bunnyHill.DownrangeWindow

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

            validateWindow bunnyHill.DownrangeWindow

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
            new IProjectionMap<BunnyHillSettings,BunnyHillChange> with
                member __.Change projection systemContext changeRequest =
                    change projection systemContext changeRequest

                member __.Constrain projection systemContext =
                    constrain projection systemContext

                member __.ToAcquisitionSettings projection systemContext =
                    toSettings projection systemContext
        }
