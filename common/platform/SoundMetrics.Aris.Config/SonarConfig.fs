// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Config

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

[<Measure>] type Us // Microseconds

type PingMode = uint32

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

    [<CompiledName("RangeContains")>]
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

    let SampleCountRange =      range "SampleCount"          128u         4096u
    let FocusPositionRange =    range "FocusPosition"          0u         1000u
    let ReceiverGainRange =     range "ReceiverGain"           0u           24u
    let FrameRateRange =        range "FrameRate"           1.0f</s>     15.0f</s>

    let SampleStartDelayRange = range "SampleStartDelay"     930<Us>     60000<Us>
    let CyclePeriodRange =      range "CyclePeriod"         1802<Us>    150000<Us>
    let SamplePeriodRange =     range "SamplePeriod"           4<Us>       100<Us>
    let PulseWidthRange =       range "PulseWidth"             4<Us>        80<Us>

    let WindowStartRange =      range "WindowStart"          0.7<m>       40.0<m>
    let WindowEndRange =        range "WindowEnd"            1.3<m>      100.0<m>

    let CyclePeriodMargin = 360<Us>

    type SystemTypeRanges = {
        SystemType:             SystemType
        PulseWidthRange:        Range<int<Us>>
        SampleStartDelayRange:  Range<int<Us>>
        SamplePeriodRange:      Range<int<Us>>
        CyclePeriodRange:       Range<int<Us>>
        WindowStartRange:       Range<float<m>>
        WindowEndRange:         Range<float<m>>
    }

    let systemTypeRangeMap =
        [ { SystemType = SystemType.Aris1800
            PulseWidthRange =       constrainRangeMax PulseWidthRange          40<Us>
            SampleStartDelayRange = constrainRangeMax SampleStartDelayRange 36000<Us>
            SamplePeriodRange =     constrainRangeMax SamplePeriodRange        32<Us>
            CyclePeriodRange =      constrainRangeMax CyclePeriodRange      80000<Us>
            WindowStartRange =      constrainRangeMax WindowStartRange       25.0<m>
            WindowEndRange =        constrainRangeMax WindowEndRange         50.0<m>
          }
          { SystemType = SystemType.Aris3000
            PulseWidthRange =       constrainRangeMax PulseWidthRange          24<Us>
            SampleStartDelayRange = constrainRangeMax SampleStartDelayRange 18000<Us>
            SamplePeriodRange =     constrainRangeMax SamplePeriodRange        26<Us>
            CyclePeriodRange =      constrainRangeMax CyclePeriodRange      40000<Us>
            WindowStartRange =      constrainRangeMax WindowStartRange       12.0<m>
            WindowEndRange =        constrainRangeMax WindowEndRange         20.0<m>
          }
          { SystemType = SystemType.Aris1200
            PulseWidthRange =       constrainRangeMax PulseWidthRange          80<Us>
            SampleStartDelayRange = constrainRangeMax SampleStartDelayRange 60000<Us>
            SamplePeriodRange =     constrainRangeMax SamplePeriodRange        40<Us>
            CyclePeriodRange =      constrainRangeMax CyclePeriodRange     150000<Us>
            WindowStartRange =      constrainRangeMax WindowStartRange       40.0<m>
            WindowEndRange =        constrainRangeMax WindowEndRange        100.0<m>
          } ]
        |> List.map (fun elem -> elem.SystemType, elem)
        |> Map.ofList

    type PingModeConfig = {
        PingMode: PingMode
        IsSupported: bool
        ChannelCount: uint32
        PingsPerFrame: uint32
    }

    let pingModeConfigurations =
        [ { PingMode =  1u; IsSupported = true ; ChannelCount =  48u; PingsPerFrame = 3u }
          { PingMode =  2u; IsSupported = false; ChannelCount =  48u; PingsPerFrame = 1u }
          { PingMode =  3u; IsSupported = true ; ChannelCount =  96u; PingsPerFrame = 6u }
          { PingMode =  4u; IsSupported = false; ChannelCount =  96u; PingsPerFrame = 2u }
          { PingMode =  5u; IsSupported = false; ChannelCount =  96u; PingsPerFrame = 1u }
          { PingMode =  6u; IsSupported = true ; ChannelCount =  64u; PingsPerFrame = 4u }
          { PingMode =  7u; IsSupported = false; ChannelCount =  64u; PingsPerFrame = 2u }
          { PingMode =  8u; IsSupported = false; ChannelCount =  64u; PingsPerFrame = 1u }
          { PingMode =  9u; IsSupported = true ; ChannelCount = 128u; PingsPerFrame = 8u }
          { PingMode = 10u; IsSupported = false; ChannelCount = 128u; PingsPerFrame = 4u }
          { PingMode = 11u; IsSupported = false; ChannelCount = 128u; PingsPerFrame = 2u }
          { PingMode = 12u; IsSupported = false; ChannelCount = 128u; PingsPerFrame = 1u } ]
          |> List.map (fun elem -> elem.PingMode, elem)
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

    [<CompiledName("GetPingModeConfig")>]
    let getPingModeConfig pingMode =
        if pingModeConfigurations.ContainsKey pingMode then
            pingModeConfigurations.[pingMode]
        else
            invalidArg "pingMode" "unexpected ping mode"

    [<CompiledName("GetDefaultPingModeForSystemType")>]
    let getDefaultPingModeForSystemType systemType =
        if systemTypeToPingModeMap.ContainsKey systemType then
            systemTypeToPingModeMap.[systemType].defaultPingMode
        else
            invalidArg "systemType" "unexpected system type"
