// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

type Frequency = Low = 0 | High = 1

type SystemType =
    | Aris1800 = 0
    | Aris3000 = 1
    | Aris1200 = 2
    | DidsonStd = 3
    | DidsonLR = 4

module SonarConfig =

    type Range<'t> = { name: string; min: 't; max: 't }
    with
        override rng.ToString() = sprintf "%s %A-%A" rng.name rng.min rng.max

    let contains value range =
        assert (range.min <= range.max)
        range.min <= value && value <= range.max


    module internal RangeImpl =

        let inline range<'T when 'T : comparison> name (min: 'T) (max: 'T) = { name = name; min = min; max = max }


        let inline isSubrangeOf<'T when 'T : comparison> (original : Range<'T>) subrange =

            original |> contains subrange.min && original |> contains subrange.max


        let inline subrangeOf<'T when 'T : comparison> range (min : 'T) (max : 'T) =

            let subrange = { name = range.name; min = min; max = max }
            if not (subrange |> isSubrangeOf range) then
                invalidArg "min" "subrange falls outside original range"

            subrange


        let inline constrainRangeMax<'T when 'T : comparison> range (max : 'T) = subrangeOf range range.min max

    open RangeImpl

    // Min/max values per ARIS Engineering Test Command List

    let sampleCountRange =      range "SampleCount"          128u         4096u
    let focusPositionRange =    range "FocusPosition"          0u         1000u
    let receiverGainRange =     range "ReceiverGain"           0u           24u
    let frameRateRange =        range "FrameRate"           1.0f</s>     15.0f</s>

    let sampleStartDelayRange = range "SampleStartDelay"     930<Us>     60000<Us>
    let cyclePeriodRange =      range "CyclePeriod"         1802<Us>    150000<Us>
    let samplePeriodRange =     range "SamplePeriod"           4<Us>       100<Us>
    let pulseWidthRange =       range "PulseWidth"             4<Us>        80<Us>

    let windowStartRange =      range "WindowStart"          0.7<m>       40.0<m>
    let windowEndRange =        range "WindowEnd"            1.3<m>      100.0<m>

    let cyclePeriodMargin = 360<Us>

    type SystemTypeRanges = {
        systemType:             SystemType
        pulseWidthRange:        Range<int<Us>>
        sampleStartDelayRange:  Range<int<Us>>
        samplePeriodRange:      Range<int<Us>>
        cyclePeriodRange:       Range<int<Us>>
        windowStartRange:       Range<float<m>>
        windowEndRange:         Range<float<m>>
    }

    let systemTypeRangeMap =
        [ { systemType = SystemType.Aris1800
            pulseWidthRange =       constrainRangeMax pulseWidthRange          40<Us>
            sampleStartDelayRange = constrainRangeMax sampleStartDelayRange 36000<Us>
            samplePeriodRange =     constrainRangeMax samplePeriodRange        32<Us>
            cyclePeriodRange =      constrainRangeMax cyclePeriodRange      80000<Us>
            windowStartRange =      constrainRangeMax windowStartRange       25.0<m>
            windowEndRange =        constrainRangeMax windowEndRange         50.0<m>
          }
          { systemType = SystemType.Aris3000
            pulseWidthRange =       constrainRangeMax pulseWidthRange          24<Us>
            sampleStartDelayRange = constrainRangeMax sampleStartDelayRange 18000<Us>
            samplePeriodRange =     constrainRangeMax samplePeriodRange        26<Us>
            cyclePeriodRange =      constrainRangeMax cyclePeriodRange      40000<Us>
            windowStartRange =      constrainRangeMax windowStartRange       12.0<m>
            windowEndRange =        constrainRangeMax windowEndRange         20.0<m>
          }
          { systemType = SystemType.Aris1200
            pulseWidthRange =       constrainRangeMax pulseWidthRange          80<Us>
            sampleStartDelayRange = constrainRangeMax sampleStartDelayRange 60000<Us>
            samplePeriodRange =     constrainRangeMax samplePeriodRange        40<Us>
            cyclePeriodRange =      constrainRangeMax cyclePeriodRange     150000<Us>
            windowStartRange =      constrainRangeMax windowStartRange       40.0<m>
            windowEndRange =        constrainRangeMax windowEndRange        100.0<m>
          } ]
        |> List.map (fun elem -> elem.systemType, elem)
        |> Map.ofList

    type PingModeConfig = {
        pingMode: PingMode
        isSupported: bool
        channelCount: uint32
        pingsPerFrame: uint32
    }

    let pingModeConfigurations =
        [ { pingMode =  1u; isSupported = true ; channelCount =  48u; pingsPerFrame = 3u }
          { pingMode =  2u; isSupported = false; channelCount =  48u; pingsPerFrame = 1u }
          { pingMode =  3u; isSupported = true ; channelCount =  96u; pingsPerFrame = 6u }
          { pingMode =  4u; isSupported = false; channelCount =  96u; pingsPerFrame = 2u }
          { pingMode =  5u; isSupported = false; channelCount =  96u; pingsPerFrame = 1u }
          { pingMode =  6u; isSupported = true ; channelCount =  64u; pingsPerFrame = 4u }
          { pingMode =  7u; isSupported = false; channelCount =  64u; pingsPerFrame = 2u }
          { pingMode =  8u; isSupported = false; channelCount =  64u; pingsPerFrame = 1u }
          { pingMode =  9u; isSupported = true ; channelCount = 128u; pingsPerFrame = 8u }
          { pingMode = 10u; isSupported = false; channelCount = 128u; pingsPerFrame = 4u }
          { pingMode = 11u; isSupported = false; channelCount = 128u; pingsPerFrame = 2u }
          { pingMode = 12u; isSupported = false; channelCount = 128u; pingsPerFrame = 1u } ]
          |> List.map (fun elem -> elem.pingMode, elem)
          |> Map.ofList

    type SystemTypePingModeInfo =
        { systemType: SystemType; defaultPingMode: PingMode; validPingModes: PingMode list }

    let systemTypeToPingModeMap = 
        [ { systemType = SystemType.Aris1200;  defaultPingMode = 1u; validPingModes = [ 1u ] }
          { systemType = SystemType.Aris1800;  defaultPingMode = 3u; validPingModes = [ 1u; 3u ] }
          { systemType = SystemType.Aris3000;  defaultPingMode = 9u; validPingModes = [ 6u; 9u ] }
          // { systemType = SystemType.DidsonStd; defaultPingMode = 1; validPingModes = [ 1; 2 ] }
          // { systemType = SystemType.DidsonLR;  defaultPingMode = 1; validPingModes = [ 1; 2 ] }
          ]
          |> List.map (fun elem -> elem.systemType, elem)
          |> Map.ofList

    let getPingModeConfig pingMode =
        if pingModeConfigurations.ContainsKey pingMode then
            pingModeConfigurations.[pingMode]
        else
            invalidArg "pingMode" "unexpected ping mode"

    let getDefaultPingModeForSystemType systemType =
        if systemTypeToPingModeMap.ContainsKey systemType then
            systemTypeToPingModeMap.[systemType].defaultPingMode
        else
            invalidArg "systemType" "unexpected system type"
