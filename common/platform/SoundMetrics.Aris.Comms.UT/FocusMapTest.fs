namespace SoundMetrics.Aris.Comms.UT

open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Aris.Comms
open SoundMetrics.Aris.Comms.FocusMap
open SoundMetrics.Aris.Comms.FocusMapDetails
open SoundMetrics.Aris.Comms.FocusMapTypes
open System

[<TestClass>]
type FocusMapTest () =

    let testMapPoints systemType telephoto fifthDistance eigthColdFUs =

        // In fresh water
        let triplet = getFocusMapTriplet systemType telephoto
        printfn "First fresh distance: %f" triplet.FreshMap.FocusDistances.[0]

        Assert.AreEqual(fifthDistance, triplet.FreshMap.FocusDistances.[4])
        Assert.AreEqual(eigthColdFUs, triplet.FreshMap.FocusUnitsByTemp.[0].[7])

    [<TestMethod>]
    member __.``getFocusMapTriplet telephoto``() = testMapPoints SystemType.Aris1800 true 2.0f 49us

    [<TestMethod>]
    member __.``getFocusMapTriplet 1800``() = testMapPoints SystemType.Aris1800 false 1.6f 614us

    [<TestMethod>]
    member __.``getFocusMapTriplet 3000``() = testMapPoints SystemType.Aris3000 false 1.4f 413us

    [<TestMethod>]
    member __.``Sanity check for 1800 map``() =

        // This just checks for a known value in the 1800 map
        let range = 1.0f
        let temp = 25.0f
        let salinity = Salinity.Fresh
        let systemType = SystemType.Aris1800
        let isTelephoto = false

        let expected = 212us
        let actual = mapFocusRangeToFocusUnits  systemType range temp salinity isTelephoto
        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.``Test focus for refactor``() =

        let testCases = [|
            1.96f, 600us
            1.99f, 612us
            2.03f, 620us
            2.11f, 630us
            2.19f, 639us
            2.27f, 649us
            2.35f, 659us
            2.43f, 669us
            2.51f, 679us
            2.59f, 689us
            2.67f, 699us
            2.75f, 709us
            2.84f, 720us
            2.93f, 732us
            3.02f, 742us
            3.14f, 750us
        |]

        printfn ""
        printfn "TestFocusForRefactor"

        for range, expected in testCases do
            let actual = mapFocusRangeToFocusUnits SystemType.Aris1800 range 24.0f Salinity.Fresh false
            printfn "input=%f; expected=%u; actual=%u" range expected actual

            Assert.AreEqual(expected, actual)
