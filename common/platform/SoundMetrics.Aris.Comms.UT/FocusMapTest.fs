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
            2.19f, 640us
            2.27f, 650us
            2.35f, 660us
            2.43f, 670us
            2.51f, 680us
            2.59f, 690us
            2.67f, 700us
            2.75f, 710us
            2.84f, 721us
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

    //---------------------------------------------------------------------
    // There should be no reversal of slope (retrograde) in the ranges
    // produced by iterating over the focus units from 0-1000.
    [<TestMethod>]
    member __.``Test slope of range to focus units``() =

        let systemTests = [|
            SystemType.Aris1200, false
            SystemType.Aris1800, false
            SystemType.Aris1800, true
            SystemType.Aris3000, false
        |]

        let salinities = [| Salinity.Fresh; Salinity.Brackish; Salinity.Seawater |];
        let temperatures = [| 0.0f; 2.5f; 5.0f; 10.0f; 15.0f; 20.0f; 25.0f; 30.0f; 35.0f |]

        let getSlopeDirection (a : float32) (b : float32) =
            match a, b with
            | a, b when a < b -> -1
            | a, b when a > b -> +1
            | _ -> 0 // IsNaN(b - a) winds up here

        let allFocusUnits = [| 0us .. 1000us |]

        for systemType, isTelephoto in systemTests do
            for salinity in salinities do
                for temp in temperatures do

                    // Convert focus units to range
                    let ranges =
                        allFocusUnits
                            |> Seq.map (fun fu ->
                                mapFocusUnitsToRange systemType fu temp salinity isTelephoto)
                            |> Seq.toArray

                    let slopeDirections =
                        ranges
                        |> Seq.pairwise
                        |> Seq.map (fun (a, b) -> getSlopeDirection a b)
                        |> Seq.toArray
                    printfn "Slope directions:"
                    slopeDirections |> Seq.iter (printf "%d ")
                    printfn ""

                    let expectedSlope = slopeDirections |> Seq.filter (fun s -> s <> 0) |> Seq.head
                    let allSameSlope = slopeDirections |> Seq.forall (fun s -> s = 0 || s = expectedSlope)

                    Assert.IsTrue(allSameSlope)
