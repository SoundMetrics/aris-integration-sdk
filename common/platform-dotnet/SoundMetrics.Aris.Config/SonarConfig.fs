// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Config

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

[<Measure>] type Us // Microseconds

type PingMode = PingMode1 | PingMode3 | PingMode6 | PingMode9 | InvalidPingMode of uint32
with
    member pm.IsSupported =
        match pm with
        | PingMode1 | PingMode3 | PingMode6 | PingMode9 -> true
        | InvalidPingMode _ -> false

    member pm.ToUInt32 () =
        match pm with
        | PingMode1 -> 1u
        | PingMode3 -> 3u
        | PingMode6 -> 6u
        | PingMode9 -> 9u
        | InvalidPingMode pingMode -> failwithf "Unexpected use of InvalidPingMode: %u" pingMode

    static member From (pingMode : uint32) =
        match pingMode with
        | 1u -> PingMode1
        | 3u -> PingMode3
        | 6u -> PingMode6
        | 9u -> PingMode9
        | _ -> invalidArg "pingMode" "Unsupported ping mode"

type Frequency = Low = 0 | High = 1

module SonarConfig =

    type Range<'t> = { Name: string; Min: 't; Max: 't }
    with
        override rng.ToString() = sprintf "%s %A-%A" rng.Name rng.Min rng.Max

    [<CompiledName("RangeContains")>]
    let contains value range =
        assert (range.Min <= range.Max)
        range.Min <= value && value <= range.Max

    module internal RangeImpl =

        let inline range<'T when 'T : comparison> name (min: 'T) (max: 'T) = { Name = name; Min = min; Max = max }


        let inline isSubrangeOf<'T when 'T : comparison> (original : Range<'T>) subrange =

            original |> contains subrange.Min && original |> contains subrange.Max


        let inline subrangeOf<'T when 'T : comparison> range (min : 'T) (max : 'T) =

            let subrange = { Name = range.Name; Min = min; Max = max }
            if not (subrange |> isSubrangeOf range) then
                invalidArg "min" "subrange falls outside original range"

            subrange


        let inline constrainRangeMax<'T when 'T : comparison> range (max : 'T) = subrangeOf range range.Min max

    open RangeImpl
    open SoundMetrics.Common

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
        SystemType:             ArisSystemType
        PulseWidthRange:        Range<int<Us>>
        SampleStartDelayRange:  Range<int<Us>>
        SamplePeriodRange:      Range<int<Us>>
        CyclePeriodRange:       Range<int<Us>>
        WindowStartRange:       Range<float<m>>
        WindowEndRange:         Range<float<m>>
    }

    let systemTypeRangeMap =
        [ { SystemType =            ArisSystemType.Aris1800
            PulseWidthRange =       constrainRangeMax PulseWidthRange          40<Us>
            SampleStartDelayRange = constrainRangeMax SampleStartDelayRange 36000<Us>
            SamplePeriodRange =     constrainRangeMax SamplePeriodRange        32<Us>
            CyclePeriodRange =      constrainRangeMax CyclePeriodRange      80000<Us>
            WindowStartRange =      constrainRangeMax WindowStartRange       25.0<m>
            WindowEndRange =        constrainRangeMax WindowEndRange         50.0<m>
          }
          { SystemType =            ArisSystemType.Aris3000
            PulseWidthRange =       constrainRangeMax PulseWidthRange          24<Us>
            SampleStartDelayRange = constrainRangeMax SampleStartDelayRange 18000<Us>
            SamplePeriodRange =     constrainRangeMax SamplePeriodRange        26<Us>
            CyclePeriodRange =      constrainRangeMax CyclePeriodRange      40000<Us>
            WindowStartRange =      constrainRangeMax WindowStartRange       12.0<m>
            WindowEndRange =        constrainRangeMax WindowEndRange         20.0<m>
          }
          { SystemType =            ArisSystemType.Aris1200
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
        ChannelCount: uint32
        PingsPerFrame: uint32
    }

    let pingModeConfigurations =
        [ { PingMode = PingMode1;           ChannelCount =  48u; PingsPerFrame = 3u }
          { PingMode = InvalidPingMode 2u;  ChannelCount =  48u; PingsPerFrame = 1u }
          { PingMode = PingMode3;           ChannelCount =  96u; PingsPerFrame = 6u }
          { PingMode = InvalidPingMode 4u;  ChannelCount =  96u; PingsPerFrame = 2u }
          { PingMode = InvalidPingMode 5u;  ChannelCount =  96u; PingsPerFrame = 1u }
          { PingMode = PingMode6;           ChannelCount =  64u; PingsPerFrame = 4u }
          { PingMode = InvalidPingMode 7u;  ChannelCount =  64u; PingsPerFrame = 2u }
          { PingMode = InvalidPingMode 8u;  ChannelCount =  64u; PingsPerFrame = 1u }
          { PingMode = PingMode9;           ChannelCount = 128u; PingsPerFrame = 8u }
          { PingMode = InvalidPingMode 10u; ChannelCount = 128u; PingsPerFrame = 4u }
          { PingMode = InvalidPingMode 11u; ChannelCount = 128u; PingsPerFrame = 2u }
          { PingMode = InvalidPingMode 12u; ChannelCount = 128u; PingsPerFrame = 1u } ]
          |> List.map (fun elem -> elem.PingMode, elem)
          |> Map.ofList

    type SystemTypePingModeInfo =
        { systemType: ArisSystemType; defaultPingMode: PingMode; validPingModes: PingMode list }

    let systemTypeToPingModeMap = 
        [ { systemType = ArisSystemType.Aris1200;  defaultPingMode = PingMode1; validPingModes = [ PingMode1 ] }
          { systemType = ArisSystemType.Aris1800;  defaultPingMode = PingMode3; validPingModes = [ PingMode1; PingMode3 ] }
          { systemType = ArisSystemType.Aris3000;  defaultPingMode = PingMode9; validPingModes = [ PingMode6; PingMode9 ] }
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
