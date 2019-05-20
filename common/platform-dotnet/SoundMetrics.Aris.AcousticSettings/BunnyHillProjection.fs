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

    [<CompiledName("ApplyBunnyHillChange")>]
    let applyBunnyHillChange =

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


        let toSettings _systemContext (projection: BunnyHillProjection) : AcousticSettings =

            validateWindow projection.DownrangeWindow
            AcousticSettings.Invalid

        let applyChange systemContext projection change : struct (BunnyHillProjection * AcousticSettings) =

            validateWindow projection.DownrangeWindow

            let newProjection =
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

            struct (newProjection, newProjection |> toSettings systemContext)

        applyChange
