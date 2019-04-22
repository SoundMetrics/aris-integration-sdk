namespace SoundMetrics.Aris.AcousticSettings

open SoundMetrics.Aris.AcousticSettings.Experimental
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Microsoft.VisualStudio.TestTools.UnitTesting
open System

open BunnyHill

[<TestClass>]
type BunnyHillTests () =

    [<TestMethod>]
    member __.``Show constraint from float32 MIN/MAX`` () =
        let bh = {
            DownrangeWindow =
                { Start = Double.MinValue * 1.0<m>
                  End = Double.MaxValue * 1.0<m> } }
        let ctx = {
            SystemType = ArisSystemType.Aris3000
            WaterTemp = 20.0<degC>
            Salinity = Salinity.Seawater
            Depth = 10.0<m>
            AuxLens = AuxLensType.None
            AntialiasingPeriod = 0<Us>
        }
        let constrained = bunnyHillProjection.Constrain.Invoke(bh, ctx)

        Assert.AreNotEqual<BunnyHillSettings>(bh, constrained, "Must show *some* constraint!")

    [<TestMethod>]
    member __.``Change window start`` () =

        let bh = { DownrangeWindow = { Start = 3.0<m>; End = 5.0<m> } }
        let newWindowStart = bh.DownrangeWindow.Start + 0.1<m>
        let change = ChangeWindowStart newWindowStart
        let systemContext = {
            SystemType = ArisSystemType.Aris3000
            WaterTemp = 20.0<degC>
            Salinity = Salinity.Seawater
            Depth = 10.0<m>
            AuxLens = AuxLensType.None
            AntialiasingPeriod = 0<Us>
        }

        let changed = bunnyHillProjection.Change.Invoke(bh, systemContext, change)

        Assert.AreEqual(newWindowStart, changed.DownrangeWindow.Start)
        Assert.AreEqual(bh.DownrangeWindow.End, changed.DownrangeWindow.End)

    [<TestMethod>]
    member __.``Change window end`` () =

        let bh = { DownrangeWindow = { Start = 3.0<m>; End = 5.0<m> } }
        let newWindowEnd = bh.DownrangeWindow.End + 0.1<m>
        let change = ChangeWindowEnd newWindowEnd
        let systemContext = {
            SystemType = ArisSystemType.Aris3000
            WaterTemp = 20.0<degC>
            Salinity = Salinity.Seawater
            Depth = 10.0<m>
            AuxLens = AuxLensType.None
            AntialiasingPeriod = 0<Us>
        }

        let changed = bunnyHillProjection.Change.Invoke(bh, systemContext, change)

        Assert.AreEqual(bh.DownrangeWindow.Start, changed.DownrangeWindow.Start)
        Assert.AreEqual(newWindowEnd, changed.DownrangeWindow.End)
