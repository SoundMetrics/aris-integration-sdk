namespace SoundMetrics.Aris.AcousticSettings

open SoundMetrics.Aris.AcousticSettings.Experimental
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Microsoft.VisualStudio.TestTools.UnitTesting
open System

[<TestClass>]
type ProjectionChangeTests () =

    [<TestMethod>]
    member __.``Invalid downrange window`` () =
        let bh = { DownrangeWindow = { Start = 1.0<m>; End = 1.0<m> } }
        let ctx = {
            SystemType = ArisSystemType.Aris3000
            WaterTemp = 20.0<degC>
            Salinity = Salinity.Seawater
            Depth = 10.0<m>
            AuxLens = AuxLensType.None
        }

        let action = Action(fun () ->
            SettingsProjection.mapProjectionToSettings
                ctx
                BunnyHill.bunnyHillMapping
                bh
                (ChangeWindowStart 1.0<m>)
            |> ignore
        )
        let ex = Assert.ThrowsException<ArgumentException>(action)
        Assert.IsNotNull(ex)
