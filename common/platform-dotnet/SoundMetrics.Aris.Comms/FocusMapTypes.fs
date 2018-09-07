// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

// DESIGN NOTE
//
// As of this writing we have 3 focus maps for ARIS: 3000, 1200/1800, and telephoto.
// (These maps are represented in FocusMaps.inl.)
//
// Each map illustrates the mapping of a particular focus range to "focus units," which
// are used to communicate where to set focus. (These values fall in the range 0-1000.)
//
// Each focus map contains accommodates three different salinities: 0, 15, and 25 for
// fresh, brackish, and saltwater, respectively.
//
// The maps also accommodate a range of temperatures. Both temperature and salinity affect
// the speed of sound in water which, in turn, affects how we must focus the lens.
//
// In order to interpolate between the various axes--range, temperature, and salinity--
// we take things one step at a time. E.g., when converting from range to focus units
// here, more or less, is how it's done.
//
// - Select a focus table based on system type and salinity
// - Select data points encompassing the input range and temperature.For example, if the
//   desired focus range is 2.7m and the measured water temperature is 12 °C, then the
//   focus table data for D1[2.6m, 10 °C], D2[2.8m, 10 °C], D3[2.6m, 12.5 °C],
//   D4[2.8m, 12.5 °C] would be used.
// - First calculate the interpolated data for the exact range at 10 °C and 12.5 °C
//      D5 = D1 + (Range – 2.6) / 0.2 * (D2 – D1)
//      D6 = D3 + (Range – 2.6) / 0.2 * (D4 – D3)
// - Then calculate the result interpolated for the measured temperature
//      Result = D5 + (Temp – 10) / 2.5 * (D6 – D5)
//
// Note that in the code we actually determine the width between the points rather than
// hard-coding 0.2 and 2.5.
//
// Note also that the focus maps specify range left-to-right but that the correlated
// focus units may be either increasing or decreasing left-to-right.

module internal FocusMapTypes =

    open Serilog

    module NumericHelpers =

        [<Struct>]
        type Range<'T> = {
            Min : 'T
            Max : 'T
        }

        let minmax<'T when 'T : comparison> (l : 'T) (r : 'T) =
            let minimum = min l r
            let maximum = max l r
            { Min = minimum; Max = maximum }

        let frontback<'T> (values : 'T array) = struct (values.[0], values.[values.Length - 1])

        // Inclusive check whether 't' is within 'range'. Does not assume 'min' <= 'max'
        // as we may traverse descending sequences.
        let inRange<'T when 'T : comparison> (t : 'T) (left : 'T) (right : 'T) =
            let rg = minmax left right // Do not assume left <= right
            rg.Min <= t && t <= rg.Max


        // Iterate as many as (size - 1) times checking range with V[idx] and V[idx+1].
        // Expectation: no t is outside the range of values (check in getBracketIndex).
        let getEnclosingRange<'T when 'T : comparison> (t : 'T) (values : 'T array) : struct (int * int) =

            values |> Seq.pairwise
                   |> Seq.zip (Seq.initInfinite id) // index the pairs
                   |> Seq.filter (fun (_, (l, r)) -> inRange t l r)
                   |> Seq.map (fun (idx, _) -> struct (idx, idx + 1))
                   |> Seq.head

        // This function determines which elements in 'values' bracket the given value 't'.
        // The indices of the two bracketing elements are returned. If 't' is not within the
        // given 'values' then the closest valid bracket is given. Focus maps do not all cover
        // the entire range with valid focus units, so we must constrain to the nearest valid.
        let getBracketIndex<'T when 'T : comparison> (t : 'T) (values : 'T array) : struct (int * int) =

            let idxBack = values.Length - 1
            let front = values.[0]
            let back = values.[idxBack]

            if front < back then
                // Increasing values
                if t < front then
                    struct (0, 0)
                elif t > back then
                    struct (idxBack, idxBack)
                else
                    getEnclosingRange t values
            else
                // Decreasing values
                if t > front then
                    struct (0, 0)
                elif t < back then
                    struct (idxBack, idxBack)
                else
                    getEnclosingRange t values


        let interpolate (input : float32) struct (inputL : float32, outputL : float32) struct (inputR : float32, outputR : float32) =

            let inputRange = inputR - inputL
            let outputRange = outputR - outputL

            let value =
                if inputRange = 0.0f then
                    outputL
                else
                    let inputRatio = (input - inputL) / inputRange
                    outputL + (inputRatio * outputRange)

            Log.Verbose (sprintf "interpolate %A inputRange=[%A - %A]; outputRange=[%A - %A] -> %A"
                            input inputL inputR outputL outputR value)

            value


        let closerBound (target : float) (lb : float) (ub : float) =

            if abs (lb - target) < abs (target - ub) then
                lb
            else
                ub


    open NumericHelpers

    type FU = uint16
    type FocusUnitList = FU array

    let buildFocusUnitMinMaxes (focusUnitsByTemp : FocusUnitList array) : (FU * FU) array =

        focusUnitsByTemp
        |> Seq.map (fun fus ->
                    let struct (f, b) = frontback fus
                    let minmax = minmax f b
                    (minmax.Min, minmax.Max) )
        |> Seq.toArray

    type FocusMap = {
        Name : string
        FocusDistances : float32 array
        Temperatures : float32 array
        FocusUnitsByTemp : FocusUnitList array
        FocusUnitsMinMaxByTemp : (FU * FU) array
    }
    with
        member m.MinRange = m.FocusDistances.[0]
        member m.MaxRange = m.FocusDistances.[m.FocusDistances.Length - 1]
        override m.ToString() = sprintf "FocusMap '%s'" m.Name

    type FocusMapInputs = {
        FocusDistances : float32 array
        Temperatures : float32 array
        FocusUnitsByTemp : FocusUnitList array
    }
    with
        member i.ToFocusMap(name) =
            {
                Name = name
                FocusDistances = i.FocusDistances
                Temperatures = i.Temperatures
                FocusUnitsByTemp = i.FocusUnitsByTemp
                FocusUnitsMinMaxByTemp = buildFocusUnitMinMaxes i.FocusUnitsByTemp
            }

    type FocusMapTriplet = {
        FreshMap        : FocusMap
        BrackishMap     : FocusMap
        SaltwaterMap    : FocusMap
    }
    with
        /// Helper to ease integration with generated code.
        static member From(fresh : FocusMapInputs, brackish : FocusMapInputs, saltwater : FocusMapInputs) =
            {
                FreshMap = fresh.ToFocusMap("fresh")
                BrackishMap = brackish.ToFocusMap("brackish")
                SaltwaterMap = saltwater.ToFocusMap("saltwater")
            }
