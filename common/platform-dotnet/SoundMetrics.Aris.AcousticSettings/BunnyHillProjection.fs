// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

(*
    This is not a real form of settings.
*)

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open System

type DownrangeWindow = SoundMetrics.Aris.AcousticSettings.AcousticMath.DownrangeWindow

type BunnyHillProjection = {
    DownrangeWindow:    DownrangeWindow
}

type BunnyHillChange =
    | ChangeWindowStart of value: float<m>
    | ChangeWindowEnd of value: float<m>
    | All of BunnyHillProjection

module BunnyHill =

    [<CompiledName("BunnyHillMapping")>]
    let bunnyHillMapping =

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


        let toSettings _externalContext (projection: BunnyHillProjection) : AcousticSettings =

            validateWindow projection.DownrangeWindow
            AcousticSettings.Invalid

        let applyChange systemContext projection change : BunnyHillProjection =

            validateWindow projection.DownrangeWindow

            match change with
            | ChangeWindowStart value ->
                { projection with
                    DownrangeWindow =
                        { projection.DownrangeWindow with Start = value } }
            | ChangeWindowEnd value ->
                { projection with
                    DownrangeWindow =
                        { projection.DownrangeWindow with End = value } }
            | All newSettings -> newSettings

        let constrainProjection (systemContext: SystemContext)
                                (projection: BunnyHillProjection) =

            validateWindow projection.DownrangeWindow

            let struct (windowStartMin, windowEndMax) =
                let ranges = SoundMetrics.Aris.AcousticSettings.SonarConfig
                                .systemTypeRangeMap.[systemContext.SystemType]
                struct (ranges.WindowStartRange.Min, ranges.WindowEndRange.Max)

            let minmax f = max windowStartMin (min windowEndMax f)

            let struct (windowStart, windowEnd) =
                let struct (s, e) =
                    let downrange = projection.DownrangeWindow
                    struct (minmax downrange.Start, minmax downrange.End)
                struct (min s e, max s e)
            { DownrangeWindow = { Start = windowStart; End = windowEnd } }

        {
            new IProjectionMap<BunnyHillProjection,BunnyHillChange> with
                member __.ApplyChange systemContext projection changeRequest =
                    applyChange systemContext projection changeRequest

                member __.ConstrainProjection projection systemContext =
                    constrainProjection projection systemContext

                member __.ToAcquisitionSettings projection systemContext =
                    toSettings projection systemContext
        }
