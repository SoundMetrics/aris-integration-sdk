﻿// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SoundMetrics.Aris.AcousticSettings.UnitsOfMeasure
open SoundMetrics.Aris.Comms
open SoundMetrics.Common
open System

module internal SonarConnectionDetails =

    open SoundMetrics.Aris.AcousticSettings
    open SoundMetrics.Data
    open System.Net
    open System.Net.Sockets
    open System.Threading.Tasks.Dataflow

    type ValidatedSettings =
        | Valid of AcousticSettingsRaw
        | ValidationError of string

    module internal SettingsHelpers =

        type AcousticSettingsCookieTracker() =
            let last: AcousticSettingsCookie ref = ref 0u
            let next() = last := !last + 1u ; !last

            member __.ApplyNewCookie settings = { Cookie = next(); Settings = settings }

            type AcousticSettingsVersioned with
                member __.FromCommand (cmd: Aris.Command) =
                    if cmd.Type <> Aris.Command.Types.CommandType.SetAcoustics then
                        invalidArg "cmd" "not of type SET_ACOUSTICS"

                    let settings = cmd.Settings
                    { Cookie = settings.Cookie
                      Settings =
                          { FrameRate = float settings.FrameRate * 1.0</s>
                            SampleCount = int settings.SamplesPerBeam
                            SampleStartDelay = int settings.SampleStartDelay * 1<Us>
                            CyclePeriod = int settings.CyclePeriod * 1<Us>
                            SamplePeriod = int settings.SamplePeriod * 1<Us>
                            PulseWidth = int settings.PulseWidth * 1<Us>
                            PingMode = PingMode.From (uint32 settings.PingMode)
                            EnableTransmit = settings.EnableTransmit
                            Frequency = enum (int32 settings.Frequency)
                            Enable150Volts = settings.Enable150Volts
                            ReceiverGain = int settings.ReceiverGain } }

                member v.ToCommand () = ArisCommands.makeAcousticSettingsCmd v

        let settingsValidators =
            let simpleRangeCheck value range =
                if range |> Range.contains value then
                    None
                else
                    Some (sprintf "Value %A is out of range %s" value (range.ToString()))

            [
                fun settings -> simpleRangeCheck settings.FrameRate             SonarConfig.FrameRateRange
                fun settings -> simpleRangeCheck settings.SampleCount           SonarConfig.SampleCountRange
                fun settings -> simpleRangeCheck settings.SampleStartDelay      SonarConfig.SampleStartDelayRange
                fun settings -> simpleRangeCheck settings.CyclePeriod           SonarConfig.CyclePeriodRange
                fun settings -> simpleRangeCheck settings.SamplePeriod          SonarConfig.SamplePeriodRange
                fun settings -> simpleRangeCheck settings.PulseWidth            SonarConfig.PulseWidthRange
                fun settings -> simpleRangeCheck settings.ReceiverGain          SonarConfig.ReceiverGainRange

                fun settings -> match settings.Frequency with
                                | Frequency.Low | Frequency.High -> None
                                | _ -> Some (sprintf "Frequency %A is not Low or High" settings.Frequency)
            ]

        let validateSettings settings =
            let rec iterate validators acc =
                match validators with
                | [] -> acc
                | head :: tail ->
                    let validation = head settings
                    iterate tail (validation :: acc)

            let validationErrors = iterate settingsValidators [] |> List.choose id
            match validationErrors with
            | [] -> Valid settings
            | _ -> ValidationError (String.Join("; ", validationErrors))

    open SettingsHelpers

    let keepAlivePingInterval = TimeSpan.FromSeconds(4.0)

    open Google.Protobuf

    let getCommandLengthPrefix (cmd: Aris.Command) =
        let length : Int32 = cmd.CalculateSize()
        let msgLengthNetworkOrder = IPAddress.HostToNetworkOrder length
        BitConverter.GetBytes msgLengthNetworkOrder

    let sendCmd (socket: TcpClient) (cmd : Aris.Command) =
        // Let's not assume we're the only writer to the socket.
        // Put the prefix in the same buffer as the message.
        let prefix = getCommandLengthPrefix cmd

        use memstream = new System.IO.MemoryStream()
        memstream.Write(prefix, 0, prefix.Length)
        cmd.WriteTo(memstream)
        memstream.Flush()

        let msg = memstream.ToArray()
        if cmd.Type <> Aris.Command.Types.CommandType.Ping then
            Serilog.Log.Information("Sending command {cmdType} of {length} bytes: {cmd}",
                cmd.Type, msg.Length - prefix.Length, cmd.ToString())

        socket.Client.Send(msg)

    let buildCommandSocket (ipAddress: IPAddress) (connectionTimeout: TimeSpan) =
        if connectionTimeout < TimeSpan.FromSeconds(2.0) then
            invalidArg "connectionTimeout" "invalid connectionTimeout"

        let deadline = DateTime.Now + connectionTimeout
        let rec loop () =
            if DateTime.Now >= deadline then
                None
            else
                let client = new TcpClient()
                try
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true)
                    let ep = IPEndPoint(ipAddress, NetworkConstants.ArisSonarTcpNOListenPort) // port 56888
                    client.Connect(ep)
                    Some client
                with
                | :? System.Net.Sockets.SocketException ->
                    System.Threading.Thread.Sleep(500)
                    loop()
        loop()

    let makeCmdQueue (sendCmd: Aris.Command -> unit) =
        let cmdQueue = BufferBlock<Aris.Command>()
        let cmdProcessor = ActionBlock<Aris.Command>(fun cmd -> sendCmd cmd)
        let cmdQueueLink = cmdQueue.LinkTo(cmdProcessor)
        cmdQueue, cmdQueueLink

    /// Tracks instantaneous frame rate
    type RateTracker() =
        let lastTime: uint64 option ref = ref None

        member __.MarkNewFrame microseconds =
            let prev = lastTime.Value
            lastTime := Some(microseconds)
            match prev with
            | None -> None
            | Some prevValue -> Some(microseconds - prevValue)

    let makeMetrics instantaneousFrameRate frameStreamMetrics =
        { InstantaneousFrameRate = instantaneousFrameRate; ProtocolMetrics = frameStreamMetrics }
