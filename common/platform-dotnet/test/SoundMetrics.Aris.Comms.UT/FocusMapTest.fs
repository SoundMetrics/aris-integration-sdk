namespace SoundMetrics.Aris.Comms.UT

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Aris.Comms
open SoundMetrics.Aris.Comms.Internal.FocusMapDetails
open SoundMetrics.Common
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
    member __.``getFocusMapTriplet telephoto``() = testMapPoints ArisSystemType.Aris1800 true 2.0f 49us

    [<TestMethod>]
    member __.``getFocusMapTriplet 1800``() = testMapPoints ArisSystemType.Aris1800 false 1.6f 614us

    [<TestMethod>]
    member __.``getFocusMapTriplet 3000``() = testMapPoints ArisSystemType.Aris3000 false 1.4f 413us

    [<TestMethod>]
    member __.``Sanity check for 1800 map``() =

        // This just checks for a known value in the 1800 map
        let range = 1.0<m>
        let temp = 25.0<degC>
        let salinity = Salinity.Fresh
        let systemType = ArisSystemType.Aris1800
        let isTelephoto = false

        let expected = 212us
        let actual = (mapRangeToFocusUnits  systemType range temp salinity isTelephoto).FocusUnits
        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.``Test focus for refactor``() =

        let testCases = [|
            1.96, 600us
            1.99, 612us
            2.03, 620us
            2.11, 630us
            2.19, 640us
            2.27, 650us
            2.35, 660us
            2.43, 670us
            2.51, 680us
            2.59, 690us
            2.67, 700us
            2.75, 710us
            2.84, 721us
            2.93, 732us
            3.02, 742us
            3.14, 750us
        |]

        printfn ""
        printfn "TestFocusForRefactor"

        for range, expected in testCases do
            let range' = 1.0<m> * range
            let actual = 
                (mapRangeToFocusUnits ArisSystemType.Aris1800 range' 24.0<degC> Salinity.Fresh false).FocusUnits
            printfn "input=%f; expected=%u; actual=%u" range expected actual

            Assert.AreEqual(expected, actual)

    //---------------------------------------------------------------------
    // There should be no reversal of slope (retrograde) in the ranges
    // produced by iterating over the focus units from 0-1000.
    [<TestMethod>]
    member __.``Test slope of range to focus units``() =

        let systemTests = [|
            ArisSystemType.Aris1200, false
            ArisSystemType.Aris1800, false
            ArisSystemType.Aris1800, true
            ArisSystemType.Aris3000, false
        |]

        let salinities = [| Salinity.Fresh; Salinity.Brackish; Salinity.Seawater |];
        let temperatures = [| 0.0; 2.5; 5.0; 10.0; 15.0; 20.0; 25.0; 30.0; 35.0 |]

        let getSlopeDirection (a : float<m>, b : float<m>) =
            match a, b with
            | a, b when a < b -> -1
            | a, b when a > b -> +1
            | _ -> 0 // IsNaN(b - a) winds up here

        let allFocusUnits = seq { 0us .. 1000us } |> Seq.cache

        for systemType, isTelephoto in systemTests do
            for salinity in salinities do

                let results =
                    temperatures
                        |> Seq.map (fun temp -> async {
                            // Convert focus units to range
                            let ranges =
                                let temp' = 1.0<degC> * temp
                                allFocusUnits
                                    |> Seq.map (fun fu ->
                                        mapFocusUnitsToRange systemType fu temp' salinity isTelephoto)

                            let slopeDirections =
                                ranges
                                |> Seq.pairwise
                                |> Seq.map getSlopeDirection
                                |> Seq.cache

                            let expectedSlope = slopeDirections |> Seq.filter (fun s -> s <> 0) |> Seq.head
                            let allSameSlope = slopeDirections |> Seq.forall (fun s -> s = 0 || s = expectedSlope)

                            return allSameSlope
                                    })
                        |> Async.Parallel
                        |> Async.RunSynchronously

                // Validate
                results |> Seq.mapi (fun idx result -> struct (idx, result))
                        |> Seq.filter (fun struct (_idx, result) -> not result)
                        |> Seq.iter (fun struct (idx, _result) ->
                                Assert.Fail(
                                    sprintf "Failed at temp=%f; systemType=%A; isTelephoto=%A; salinity=%A"
                                        temperatures.[idx] systemType isTelephoto salinity)
                        )
