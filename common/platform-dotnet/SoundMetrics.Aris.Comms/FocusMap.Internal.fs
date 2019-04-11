// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.AcousticSettings
open SoundMetrics.Aris.Comms

module internal FocusUnits =
    open FocusMapTypes

    let Minimum : FU = 0us
    let Maximum : FU = 1000us

module internal FocusMapDetails =
    open FocusMapData
    open FocusMapTypes
    open NumericHelpers

    let getFocusMapTriplet (systemType : ArisSystemType) telephotoLens =

        let focusMap =
            match telephotoLens, systemType with
            | true, _ -> focusMapTelephoto
            | false, ArisSystemType.Aris3000 -> focusMap3000
            | false, _ -> focusMap12001800

        Log.Verbose (sprintf "Selected focus map for %A/telephoto=%A" systemType telephotoLens)

        focusMap

    let getFocusMap (systemType : ArisSystemType) salinity telephotoLens =

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

        let constrained = min (max lo temperature) hi
        Log.Verbose (sprintf "Constrained temperature %f to %f" temperature constrained)
        constrained

    let constrainSalinity (salinity : Salinity) : Salinity =

        let constrained =
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
        Log.Verbose (sprintf "Constrained salinity %A to %A" salinity constrained)
        constrained

    let constrainFocusUnits (focusUnits : FU) (minmax : FU * FU) =
      // Remember focus units may increase or decrease across the focus map.
      let constrained = max (fst minmax) (min (snd minmax) focusUnits)
      Log.Verbose (sprintf "Constrained FU %u to %u" focusUnits constrained)
      constrained


    [<Struct>]
    type ConstrainedUnitsLookup = {
        Range : float32
        Temperature : float32
        FocusMap : FocusMap
    }

    let lookUpConstrainUnits (systemType : ArisSystemType) (range : float32) (temp : float32) salinity telephotoLens =

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

    let lookUpConstrainRange (systemType : ArisSystemType) (focusUnits : FU) (temp : float32) salinity telephotoLens =

        let constrainedSalinity = constrainSalinity salinity

        let focusMap = getFocusMap systemType constrainedSalinity telephotoLens
        let constrainedTemp = constrainTemperature focusMap temp
        let constrainedUnits = min FocusUnits.Maximum focusUnits

        {
            FocusUnits = constrainedUnits
            Temperature = constrainedTemp
            FocusMap = focusMap
        }

    let lookUpFocusUnits (inputs : ConstrainedUnitsLookup) : FU =

        let map = inputs.FocusMap;

        Log.Verbose (sprintf "lookUpFocusUnits: temp=%A; range=%A" inputs.Temperature inputs.Range)

        // Find the four closest brackets in the focus map.

        // Get the temperature bracket
        let struct (tempIdxA, tempIdxB) = getBracketIndex inputs.Temperature map.Temperatures
        Log.Verbose (sprintf "lookUpFocusUnits: temp bracket: (tempIdxA, tempIdxB)=(%A, %A)" tempIdxA tempIdxB)

        // Get the range bracket
        let struct (rangeIdx1, rangeIdx2) = getBracketIndex inputs.Range map.FocusDistances
        Log.Verbose (sprintf "lookUpFocusUnits: temp bracket: (rangeIdx1, rangeIdx2)=(%A, %A)" rangeIdx1 rangeIdx2)

        // Get bracket point values (bracket distance, bracket temperature)
        // (selector is range, value is focus units, a/b is temperatures)

        let a1 = struct (map.FocusDistances.[rangeIdx1], float32 map.FocusUnitsByTemp.[tempIdxA].[rangeIdx1])
        let a2 = struct (map.FocusDistances.[rangeIdx2], float32 map.FocusUnitsByTemp.[tempIdxA].[rangeIdx2])
        let b1 = struct (map.FocusDistances.[rangeIdx1], float32 map.FocusUnitsByTemp.[tempIdxB].[rangeIdx1])
        let b2 = struct (map.FocusDistances.[rangeIdx2], float32 map.FocusUnitsByTemp.[tempIdxB].[rangeIdx2])

        Log.Verbose (sprintf "lookUpFocusUnits: a1=%A" a1)
        Log.Verbose (sprintf "lookUpFocusUnits: a2=%A" a2)
        Log.Verbose (sprintf "lookUpFocusUnits: b1=%A" b1)
        Log.Verbose (sprintf "lookUpFocusUnits: b2=%A" b2)


        // Interpolate a1/a2, b1/b2, then apply temperature interpolation.
        let c1 = struct (map.Temperatures.[tempIdxA], interpolate inputs.Range a1 a2)
        let c2 = struct (map.Temperatures.[tempIdxB], interpolate inputs.Range b1 b2)

        let fu = uint16 (interpolate inputs.Temperature c1 c2)
        Log.Verbose (sprintf "lookUpFocusUnits: result=%A" fu)
        fu

    let lookUpRange (inputs : ConstrainedRangeLookup) : float<m> =

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

        float (interpolate inputs.Temperature c1 c2) * 1.0<m>

    type MappedFocusUnits = {
        RequestedFocusRange: float<m>
        TemperatureC: float<degC>
        Salinity: Salinity
        Telephoto: bool
        FocusUnits: FU
        ActualFocusDistance: float<m>
    }

    // Internal only!
    let mapFocusUnitsToRange (systemType : ArisSystemType)
                             (focusUnits : FU)
                             (temperatureC : float<degC>)
                             (salinity : Salinity)
                             (telephotoLens : bool) : float<m> =

        let temp = single (temperatureC / 1.0<degC>)
        let constrainedRangeLookup =
            lookUpConstrainRange systemType focusUnits temp salinity telephotoLens
        let range = lookUpRange constrainedRangeLookup

        range

    // Internal only!
    let mapRangeToFocusUnits (systemType: ArisSystemType)
                             (range : float<m>)
                             (temperatureC : float<degC>)
                             (salinity : Salinity)
                             (telephotoLens : bool) : MappedFocusUnits =

        let temp = single (temperatureC / 1.0<degC>)
        let constrainedUnitsLookup =
            lookUpConstrainUnits systemType (single (range / 1.0<m>)) temp salinity telephotoLens
        let focusUnits = lookUpFocusUnits constrainedUnitsLookup

        { RequestedFocusRange = range
          TemperatureC = temperatureC
          Salinity = salinity
          Telephoto = telephotoLens
          FocusUnits = focusUnits
          ActualFocusDistance = mapFocusUnitsToRange systemType focusUnits temperatureC salinity telephotoLens
        }

