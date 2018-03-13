// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

open System.Text

type Salinity =
    | Fresh = 0
    | Brackish = 15
    | Seawater = 35

type AcousticSettings = {
    frameRate: float32</s>
    sampleCount: int
    sampleStartDelay: int<Us>
    cyclePeriod: int<Us>
    samplePeriod: int<Us>
    pulseWidth: int<Us>
    pingMode: PingMode
    enableTransmit: bool
    frequency: Frequency
    enable150Volts: bool
    receiverGain: float32 }
with
    override s.ToString () =
        // Using String.Format in order to avoid the pain at the intersection of sprintf and unit of measure (F# 3.1.2).
        System.String.Format(
            "frameRate={0}; sampleCount={1}; sampleStartDelay={2}; cyclePeriod={3}; samplePeriod={4}; pulseWidth={5};"
                + "pingMode={6}; enableTransmit={7}; frequency={8}; enable150Volts={9}; receiverGain={10}",
            s.frameRate, s.sampleCount, s.sampleStartDelay, s.cyclePeriod, s.samplePeriod,
            s.pulseWidth, s.pingMode, s.enableTransmit, s.frequency, s.enable150Volts, s.receiverGain)
    member s.ToShortString () =
        // Using String.Format in order to avoid the pain at the intersection of sprintf and unit of measure (F# 3.1.2).
        System.String.Format(
            "fr={0}; sc={1}; ssd={2}; cp={3}; sp={4}; pw={5}; pm={6}; tx={7}; freq={8}; 150v={9}; rcvgn={10}",
            s.frameRate, s.sampleCount, s.sampleStartDelay, s.cyclePeriod, s.samplePeriod,
            s.pulseWidth, s.pingMode, s.enableTransmit, s.frequency, s.enable150Volts, s.receiverGain)

    static member diff left right =
        let buf = StringBuilder()
        let count = ref 0
        let addDiff (name: string) l r =
            if l <> r then
                buf.AppendFormat("{0}{1}: {2} => {3}", (if !count > 0 then "; " else ""), name, l, r) |> ignore
                count := !count + 1
                
        addDiff "fr"     left.frameRate         right.frameRate
        addDiff "sc"     left.sampleCount       right.sampleCount
        addDiff "ssd"    left.sampleStartDelay  right.sampleStartDelay
        addDiff "cp"     left.cyclePeriod       right.cyclePeriod
        addDiff "sp"     left.samplePeriod      right.samplePeriod
        addDiff "pw"     left.pulseWidth        right.pulseWidth
        addDiff "pm"     left.pingMode          right.pingMode
        addDiff "tx"     left.enableTransmit    right.enableTransmit
        addDiff "freq"   left.frequency         right.frequency
        addDiff "150v"   left.enable150Volts    right.enable150Volts
        addDiff "recvgn" left.receiverGain      right.receiverGain

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
        [ SystemType.Aris1200, { frameRate = 1.0f</s>
                                 sampleCount = 1000
                                 sampleStartDelay = 4000<Us>
                                 cyclePeriod = 32400<Us>
                                 samplePeriod = 28<Us>
                                 pulseWidth = 24<Us>
                                 pingMode = getDefaultPingModeForSystemType SystemType.Aris1200
                                 enableTransmit = true
                                 frequency = Frequency.High
                                 enable150Volts = true
                                 receiverGain = 20.0f }
          SystemType.Aris1800, { frameRate = 1.0f</s>
                                 sampleCount = 1000
                                 sampleStartDelay = 2000<Us>
                                 cyclePeriod = 19400<Us>
                                 samplePeriod = 17<Us>
                                 pulseWidth = 14<Us>
                                 pingMode = getDefaultPingModeForSystemType SystemType.Aris1800
                                 enableTransmit = true
                                 frequency = Frequency.High
                                 enable150Volts = true
                                 receiverGain = 18.0f }
          SystemType.Aris3000, { frameRate = 1.0f</s>
                                 sampleCount = 1000
                                 sampleStartDelay = 1300<Us>
                                 cyclePeriod = 6700<Us>
                                 samplePeriod = 5<Us>
                                 pulseWidth = 5<Us>
                                 pingMode = getDefaultPingModeForSystemType SystemType.Aris3000
                                 enableTransmit = true
                                 frequency = Frequency.High
                                 enable150Volts = true
                                 receiverGain = 12.0f } ]
        |> Map.ofList

open RangeImpl
 
type AcousticSettings with
    static member DefaultAcousticSettingsFor systemType = SettingsImpl.defaultSettingsMap.[systemType]

    member s.Validate () =
        let pingsPerFrame = pingModeConfigurations.[s.pingMode].pingsPerFrame
        let framePeriod = int (ceil (1000000.0f / float32 s.frameRate)) * 1<Us>
        let adjustedCyclePeriod = s.sampleStartDelay + (s.samplePeriod * s.sampleCount) + 360<Us>

        let NoError = ""
        let rawResults = 
            [
                (if sampleCountRange |> contains s.sampleCount 
                    then NoError
                    else sprintf "sampleCount '%d' is out-of-range" s.sampleCount)
                (if sampleCountRange |> contains s.sampleCount
                    then NoError
                    else sprintf "sampleCount '%d' is out-of-range" s.sampleCount)
                (if sampleStartDelayRange |> contains s.sampleStartDelay
                    then NoError
                    else sprintf "sampleStartDelay '%d' is out-of-range" (int s.sampleStartDelay))
                (if cyclePeriodRange |> contains s.cyclePeriod
                    then NoError
                    else sprintf "cyclePeriod '%d' is out-of-range" (int s.cyclePeriod))
                (if samplePeriodRange |> contains s.samplePeriod
                    then NoError
                    else sprintf "samplePeriod '%d' is out-of-range" (int s.samplePeriod))
                (if pulseWidthRange |> contains s.pulseWidth
                    then NoError
                    else sprintf "pulseWidth '%d' is out-of-range" (int s.pulseWidth))
                (if receiverGainRange |> contains (int s.receiverGain)
                    then NoError
                    else sprintf "receiverGain '%f' is out-of-range" s.receiverGain)
                (if framePeriod > s.cyclePeriod * pingsPerFrame
                    then NoError
                    else sprintf "framePeriod '%d' is too small for cycle period; frame rate %f is too fast"
                                 (int framePeriod) (float s.frameRate))
                (if s.cyclePeriod >= adjustedCyclePeriod
                    then NoError
                    else sprintf "cyclePeriod '%d' < %d" (int s.cyclePeriod) (int adjustedCyclePeriod))
            ]

        let problems = rawResults |> List.filter (fun p -> p <> NoError)
        problems
