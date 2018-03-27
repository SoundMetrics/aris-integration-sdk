// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

module private FocusMapDetails =
    open FocusMapData
    open FocusMapTypes
    open NumericHelpers

    let getFocusMapTriplet (systemType : SystemType) telephotoLens =

        match telephotoLens, systemType with
        | true, _ -> focusMapTelephoto
        | false, SystemType.Aris3000 -> focusMap3000
        | false, _ -> focusMap12001800

    let getFocusMap (systemType : SystemType) salinity telephotoLens =

        let triplet = getFocusMapTriplet systemType telephotoLens

        if salinity = Salinity.Fresh then
            triplet.FreshMap
        elif salinity = Salinity.Brackish then
            triplet.BrackishMap
        else
            triplet.SaltwaterMap

    let constrainTemperature (focusMap : FocusMap) (temperature : float32) =

        let temps = focusMap.Temperatures
        let lo = temps.[0]
        let hi = temps.[temps.Length - 1]

        min (max lo temperature) hi

    let constrainSalinity (salinity : Salinity) : Salinity =

        match salinity with
        | Salinity.Fresh
        | Salinity.Brackish
        | Salinity.Seawater -> salinity
        | s when s < Salinity.Fresh -> Salinity.Fresh
        | s when s > Salinity.Seawater -> Salinity.Seawater
        | s when s < Salinity.Brackish ->
            enum (int (closerBound (float s) (float Salinity.Fresh) (float Salinity.Brackish)))
        | s ->
            enum (int (closerBound (float s) (float Salinity.Brackish) (float Salinity.Seawater)))

    let constrainFocusUnits (focusUnits : FU) (minmax : FU * FU) =
      // Remember focus units may increase or decrease across the focus map.
      max (fst minmax) (min (snd minmax) focusUnits)


    [<Struct>]
    type ConstrainedUnitsLookup = {
        Range : float32
        Temperature : float32
        FocusMap : FocusMap
    }

    let lookUpConstrainUnits (systemType : SystemType) (range : float32) (temp : float32) salinity telephotoLens =

        let constrainedSalinity = constrainSalinity salinity

        let focusMap = getFocusMap systemType constrainedSalinity telephotoLens
        let constrainedTemp = constrainTemperature focusMap temp
        let constrainedRange = max focusMap.MinRange (min focusMap.MaxRange range)

        {
            Range = constrainedRange
            Temperature = constrainedTemp
            FocusMap = focusMap
        }

    [<Struct>]
    type ConstrainedRangeLookup = {
        FocusUnits : FU
        Temperature : float32
        FocusMap : FocusMap
    }

    let lookUpConstrainRange (systemType : SystemType) (focusUnits : FU) (temp : float32) salinity telephotoLens =

        let constrainedSalinity = constrainSalinity salinity

        let focusMap = getFocusMap systemType constrainedSalinity telephotoLens
        let constrainedTemp = constrainTemperature focusMap temp
        let maxFocusUnits = uint16 1000
        let constrainedUnits = min maxFocusUnits focusUnits

        {
            FocusUnits = constrainedUnits
            Temperature = constrainedTemp
            FocusMap = focusMap
        }

    let lookUpFocusUnits (inputs : ConstrainedUnitsLookup) : FU =

        let map = inputs.FocusMap;

        // Find the four closest brackets in the focus map.

        // Get the temperature bracket
        let struct (tempIdxA, tempIdxB) = getBracketIndex inputs.Temperature map.Temperatures

        // Get the range bracket
        let struct (rangeIdx1, rangeIdx2) = getBracketIndex inputs.Range map.FocusDistances

        // Get bracket point values (bracket distance, bracket temperature)
        // (selector is range, value is focus units, a/b is temperatures)

        let a1 = struct (map.FocusDistances.[rangeIdx1], float32 map.FocusUnitsByTemp.[tempIdxA].[rangeIdx1])
        let a2 = struct (map.FocusDistances.[rangeIdx2], float32 map.FocusUnitsByTemp.[tempIdxA].[rangeIdx2])
        let b1 = struct (map.FocusDistances.[rangeIdx1], float32 map.FocusUnitsByTemp.[tempIdxB].[rangeIdx1])
        let b2 = struct (map.FocusDistances.[rangeIdx2], float32 map.FocusUnitsByTemp.[tempIdxB].[rangeIdx2])


        // Interpolate a1/a2, b1/b2, then apply temperature interpolation.
        let c1 = struct (map.Temperatures.[tempIdxA], interpolate inputs.Range a1 a2)
        let c2 = struct (map.Temperatures.[tempIdxB], interpolate inputs.Range b1 b2)

        uint16 (interpolate inputs.Temperature c1 c2)

    let lookUpRange (inputs : ConstrainedRangeLookup) =

        let map = inputs.FocusMap;

        // Find the four closest brackets in the focus map.

        // Get the temperature bracket
        let struct (tempIdxA, tempIdxB) = getBracketIndex inputs.Temperature map.Temperatures

        // Get the focus unit brackets
        let struct (unitsIdxA1, unitsIdxA2) = getBracketIndex inputs.FocusUnits map.FocusUnitsByTemp.[tempIdxA]
        let struct (unitsIdxB1, unitsIdxB2) = getBracketIndex inputs.FocusUnits map.FocusUnitsByTemp.[tempIdxB]

        // Get bracket point values
        let a1 = struct (float32 map.FocusUnitsByTemp.[tempIdxA].[unitsIdxA1], map.FocusDistances.[unitsIdxA1])
        let a2 = struct (float32 map.FocusUnitsByTemp.[tempIdxA].[unitsIdxA2], map.FocusDistances.[unitsIdxA2])
        let b1 = struct (float32 map.FocusUnitsByTemp.[tempIdxB].[unitsIdxB1], map.FocusDistances.[unitsIdxB1])
        let b2 = struct (float32 map.FocusUnitsByTemp.[tempIdxB].[unitsIdxB2], map.FocusDistances.[unitsIdxB2])

        // Interpolate focus units. Constrain the input focus units to each units-by-temp row.
        let c1 = struct (map.Temperatures.[tempIdxA],
                          interpolate (float32 (constrainFocusUnits inputs.FocusUnits map.FocusUnitsMinMaxByTemp.[tempIdxA]))
                                      a1 a2)
        let c2 = struct (map.Temperatures.[tempIdxB],
                          interpolate (float32 (constrainFocusUnits inputs.FocusUnits map.FocusUnitsMinMaxByTemp.[tempIdxB]))
                                      b1 b2)

        interpolate inputs.Temperature c1 c2

open FocusMapDetails

module FocusMap =
    open FocusMapTypes

    let mapFocusRangeToFocusUnits (systemType: SystemType)
                                  (range : float32)
                                  (temperature : float32)
                                  (salinity : Salinity)
                                  (telephotoLens : bool) : FU =

        let constrainedUnitsLookup =
            lookUpConstrainUnits systemType range temperature salinity telephotoLens
        lookUpFocusUnits constrainedUnitsLookup

    let mapFocusUnitsToRange (systemType : SystemType)
                             (focusUnits : FU)
                             (temperature : float32)
                             (salinity : Salinity)
                             (telephotoLens : bool) : float32 =

        let constrainedRangeLookup =
            lookUpConstrainRange systemType focusUnits temperature salinity telephotoLens
        let range = lookUpRange constrainedRangeLookup

        range
