// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

/// Defines the system types for ARIS: 1200, 1800, and 3000.
type ArisSystemType = Aris1800 = 0 | Aris3000 = 1 | Aris1200 = 2

type PingMode = PingMode1 | PingMode3 | PingMode6 | PingMode9 | InvalidPingMode of int
with
    member pm.IsSupported =
        match pm with
        | PingMode1 | PingMode3 | PingMode6 | PingMode9 -> true
        | InvalidPingMode _ -> false

    member pm.ToInt () =
        match pm with
        | PingMode1 -> 1
        | PingMode3 -> 3
        | PingMode6 -> 6
        | PingMode9 -> 9
        | InvalidPingMode pingMode -> failwithf "Unexpected use of InvalidPingMode: %d" pingMode

    static member From (pingMode : int) =
        match pingMode with
        | 1 -> PingMode1
        | 3 -> PingMode3
        | 6 -> PingMode6
        | 9 -> PingMode9
        | _ -> invalidArg "pingMode" (sprintf "Unsupported ping mode: %d" pingMode)

    // Convenience overload.
    static member From (pingMode : uint32) = PingMode.From (int pingMode)

type Frequency = Low = 0 | High = 1

module SonarConfig =

    open SoundMetrics.Data
    open SoundMetrics.Data.Range

    // Min/max values per ARIS Engineering Test Command List

    let SampleCountRange =      range  200          4000
    let FocusPositionRange =    range    0<vfu>     1000<vfu>
    let ReceiverGainRange =     range    0            24
    let FrameRateRange =        range  1.0</s>      15.0</s>

    let SampleStartDelayRange = range  930<Us>     60000<Us>
    let CyclePeriodRange =      range 1802<Us>    150000<Us>
    let SamplePeriodRange =     range    4<Us>       100<Us>
    let PulseWidthRange =       range    4<Us>        80<Us>

    let WindowStartRange =      range  0.7<m>       40.0<m>
    let WindowEndRange =        range  1.3<m>      100.0<m>

    let CyclePeriodMargin = 360<Us>
    let MinAntialiasing =   0<Us>

    type SystemTypeRanges = {
        SystemType:             ArisSystemType
        PulseWidthRange:        Range<int<Us>>
        SampleStartDelayRange:  Range<int<Us>>
        SamplePeriodRange:      Range<int<Us>>
        CyclePeriodRange:       Range<int<Us>>
        WindowStartRange:       Range<float<m>>
        WindowEndRange:         Range<float<m>>
    }
    with
        member this.MaxRange = this.WindowEndRange.Max

    [<CompiledName("SystemTypeRangeMap")>]
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
        PingMode:       PingMode
        ChannelCount:   int
        PingsPerFrame:  int
    }

    let pingModeConfigurations =
        [ { PingMode = PingMode1;           ChannelCount =  48;  PingsPerFrame = 3 }
          { PingMode = InvalidPingMode 2;   ChannelCount =  48;  PingsPerFrame = 1 }
          { PingMode = PingMode3;           ChannelCount =  96;  PingsPerFrame = 6 }
          { PingMode = InvalidPingMode 4;   ChannelCount =  96;  PingsPerFrame = 2 }
          { PingMode = InvalidPingMode 5;   ChannelCount =  96;  PingsPerFrame = 1 }
          { PingMode = PingMode6;           ChannelCount =  64;  PingsPerFrame = 4 }
          { PingMode = InvalidPingMode 7;   ChannelCount =  64;  PingsPerFrame = 2 }
          { PingMode = InvalidPingMode 8;   ChannelCount =  64;  PingsPerFrame = 1 }
          { PingMode = PingMode9;           ChannelCount = 128;  PingsPerFrame = 8 }
          { PingMode = InvalidPingMode 10;  ChannelCount = 128;  PingsPerFrame = 4 }
          { PingMode = InvalidPingMode 11;  ChannelCount = 128;  PingsPerFrame = 2 }
          { PingMode = InvalidPingMode 12;  ChannelCount = 128;  PingsPerFrame = 1 } ]
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
