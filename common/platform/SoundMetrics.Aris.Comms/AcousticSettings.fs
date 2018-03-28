// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

open System.Text

type Salinity =
    | Fresh = 0
    | Brackish = 15
    | Seawater = 35

type AcousticSettings = {
    FrameRate: float32</s>
    SampleCount: uint32
    SampleStartDelay: int<Us>
    CyclePeriod: int<Us>
    SamplePeriod: int<Us>
    PulseWidth: int<Us>
    PingMode: PingMode
    EnableTransmit: bool
    Frequency: Frequency
    Enable150Volts: bool
    ReceiverGain: float32 }
with
    override s.ToString () =
        // Using String.Format in order to avoid the pain at the intersection of sprintf and unit of measure (F# 3.1.2).
        System.String.Format(
            "frameRate={0}; sampleCount={1}; sampleStartDelay={2}; cyclePeriod={3}; samplePeriod={4}; pulseWidth={5};"
                + "pingMode={6}; enableTransmit={7}; frequency={8}; enable150Volts={9}; receiverGain={10}",
            s.FrameRate, s.SampleCount, s.SampleStartDelay, s.CyclePeriod, s.SamplePeriod,
            s.PulseWidth, s.PingMode, s.EnableTransmit, s.Frequency, s.Enable150Volts, s.ReceiverGain)
    member s.ToShortString () =
        // Using String.Format in order to avoid the pain at the intersection of sprintf and unit of measure (F# 3.1.2).
        System.String.Format(
            "fr={0}; sc={1}; ssd={2}; cp={3}; sp={4}; pw={5}; pm={6}; tx={7}; freq={8}; 150v={9}; rcvgn={10}",
            s.FrameRate, s.SampleCount, s.SampleStartDelay, s.CyclePeriod, s.SamplePeriod,
            s.PulseWidth, s.PingMode, s.EnableTransmit, s.Frequency, s.Enable150Volts, s.ReceiverGain)

    static member diff left right =
        let buf = StringBuilder()
        let count = ref 0
        let addDiff (name: string) l r =
            if l <> r then
                buf.AppendFormat("{0}{1}: {2} => {3}", (if !count > 0 then "; " else ""), name, l, r) |> ignore
                count := !count + 1
                
        addDiff "fr"     left.FrameRate         right.FrameRate
        addDiff "sc"     left.SampleCount       right.SampleCount
        addDiff "ssd"    left.SampleStartDelay  right.SampleStartDelay
        addDiff "cp"     left.CyclePeriod       right.CyclePeriod
        addDiff "sp"     left.SamplePeriod      right.SamplePeriod
        addDiff "pw"     left.PulseWidth        right.PulseWidth
        addDiff "pm"     left.PingMode          right.PingMode
        addDiff "tx"     left.EnableTransmit    right.EnableTransmit
        addDiff "freq"   left.Frequency         right.Frequency
        addDiff "150v"   left.Enable150Volts    right.Enable150Volts
        addDiff "recvgn" left.ReceiverGain      right.ReceiverGain

        buf.ToString()

/// Represents a versioned AcoustincSettings; a cookie is used to uniquely identify
/// a specific AcousticSettings so the protocols can refer to settings commands.
type AcousticSettingsVersioned = { cookie: AcousticSettingsCookie; settings: AcousticSettings }

/// Used to track the latest acoustic settings applied.
type AcousticSettingsApplied =
    | Uninitialized
    | Applied of AcousticSettingsVersioned
    | Constrained of AcousticSettingsVersioned
    | Invalid of cookie: AcousticSettingsCookie * settings: AcousticSettings

open SonarConfig

module internal SettingsImpl =
    let defaultSettingsMap =
        [ SystemType.Aris1200, { FrameRate = 1.0f</s>
                                 SampleCount = 1000u
                                 SampleStartDelay = 4000<Us>
                                 CyclePeriod = 32400<Us>
                                 SamplePeriod = 28<Us>
                                 PulseWidth = 24<Us>
                                 PingMode = getDefaultPingModeForSystemType SystemType.Aris1200
                                 EnableTransmit = true
                                 Frequency = Frequency.High
                                 Enable150Volts = true
                                 ReceiverGain = 20.0f }
          SystemType.Aris1800, { FrameRate = 1.0f</s>
                                 SampleCount = 1000u
                                 SampleStartDelay = 2000<Us>
                                 CyclePeriod = 19400<Us>
                                 SamplePeriod = 17<Us>
                                 PulseWidth = 14<Us>
                                 PingMode = getDefaultPingModeForSystemType SystemType.Aris1800
                                 EnableTransmit = true
                                 Frequency = Frequency.High
                                 Enable150Volts = true
                                 ReceiverGain = 18.0f }
          SystemType.Aris3000, { FrameRate = 1.0f</s>
                                 SampleCount = 1000u
                                 SampleStartDelay = 1300<Us>
                                 CyclePeriod = 6700<Us>
                                 SamplePeriod = 5<Us>
                                 PulseWidth = 5<Us>
                                 PingMode = getDefaultPingModeForSystemType SystemType.Aris3000
                                 EnableTransmit = true
                                 Frequency = Frequency.High
                                 Enable150Volts = true
                                 ReceiverGain = 12.0f } ]
        |> Map.ofList

open RangeImpl
 
type AcousticSettings with
    static member DefaultAcousticSettingsFor systemType = SettingsImpl.defaultSettingsMap.[systemType]

    member s.Validate () =
        let pingsPerFrame = pingModeConfigurations.[s.PingMode].pingsPerFrame
        let framePeriod = int (ceil (1000000.0f / float32 s.FrameRate)) * 1<Us>
        let adjustedCyclePeriod = s.SampleStartDelay + (s.SamplePeriod * int s.SampleCount) + 360<Us>

        let NoError = ""
        let rawResults = 
            [
                (if sampleCountRange |> contains s.SampleCount 
                    then NoError
                    else sprintf "sampleCount '%d' is out-of-range" s.SampleCount)
                (if sampleCountRange |> contains s.SampleCount
                    then NoError
                    else sprintf "sampleCount '%d' is out-of-range" s.SampleCount)
                (if sampleStartDelayRange |> contains s.SampleStartDelay
                    then NoError
                    else sprintf "sampleStartDelay '%d' is out-of-range" (int s.SampleStartDelay))
                (if cyclePeriodRange |> contains s.CyclePeriod
                    then NoError
                    else sprintf "cyclePeriod '%d' is out-of-range" (int s.CyclePeriod))
                (if samplePeriodRange |> contains s.SamplePeriod
                    then NoError
                    else sprintf "samplePeriod '%d' is out-of-range" (int s.SamplePeriod))
                (if pulseWidthRange |> contains s.PulseWidth
                    then NoError
                    else sprintf "pulseWidth '%d' is out-of-range" (int s.PulseWidth))
                (if receiverGainRange |> contains (uint32 s.ReceiverGain)
                    then NoError
                    else sprintf "receiverGain '%f' is out-of-range" s.ReceiverGain)
                (if framePeriod > s.CyclePeriod * int pingsPerFrame
                    then NoError
                    else sprintf "framePeriod '%d' is too small for cycle period; frame rate %f is too fast"
                                 (int framePeriod) (float s.FrameRate))
                (if s.CyclePeriod >= adjustedCyclePeriod
                    then NoError
                    else sprintf "cyclePeriod '%d' < %d" (int s.CyclePeriod) (int adjustedCyclePeriod))
            ]

        let problems = rawResults |> List.filter (fun p -> p <> NoError)
        problems
