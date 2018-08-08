// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SoundMetrics.Aris.Comms.Internal

module FocusRange =
    open FocusMapDetails

    type AvailableFocusRange = {
        Min: float<m>
        Max: float<m>
    }

    [<CompiledName("CalculateAvailableFocusRange")>]
    let calculateAvailableFocusRange systemType temperatureC salinity telephoto : AvailableFocusRange =
        let min = mapFocusUnitsToRange systemType FocusUnits.Minimum temperatureC salinity telephoto
        let max = mapFocusUnitsToRange systemType FocusUnits.Maximum temperatureC salinity telephoto

        // Focus maps go both directions: from 0-1000 and 1000-0, so put the min
        // and max ranges in the right order.
        if min < max then
            { Min = min; Max = max }
        else
            { Min = max; Max = min }
