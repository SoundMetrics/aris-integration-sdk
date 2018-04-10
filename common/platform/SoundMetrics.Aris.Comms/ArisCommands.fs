// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Serilog
open System
open System.Net

module ArisCommands =

    /// Sets the sonar clock to the local time of the value passed in.
    let makeSetDatetimeCmd (dateTimeOffset: DateTimeOffset) =
        let localTime = dateTimeOffset.ToLocalTime()
        let dateTimeString =
            localTime.ToString("yyyy'-'MMM'-'dd HH':'mm':'ss",
                               System.Globalization.CultureInfo.InvariantCulture)
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetDatetime,
            DateTime =
                Aris.Command.Types.SetDateTime(
                DateTime = dateTimeString
            )
        )

    /// Sets the sonar clock to the current local time.
    let makeSetDatetimeCmdAuto () =
        let now = DateTimeOffset.Now
        makeSetDatetimeCmd (now.ToLocalTime())

    let makeFramestreamReceiverCmd (ep : IPEndPoint) =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetFramestreamReceiver,
            FrameStreamReceiver = Aris.Command.Types.SetFrameStreamReceiver(
                Ip = ep.Address.ToString(),
                Port = uint32 ep.Port
            )
        )

    let mapFocusRange systemType range degreesC salinity telephoto =

        if Double.IsNaN(float range) || Double.IsInfinity(float range) then
            invalidArg "range" (sprintf "Value '%f' is invalid" (float range))

        FocusMap.mapRangeToFocusUnits systemType range degreesC salinity telephoto

    let makeFocusCmd (requestedFocus: FU) =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetFocus,
            FocusPosition = Aris.Command.Types.SetFocusPosition(
                Position = uint32 requestedFocus
            )
        )

    let makeSetRotatorMountCmd (mountType: RotatorMount) =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetRotatorMount,
            Mount = Aris.Command.Types.SetRotatorMount(
                Mount = enum (int mountType)
            )
        )

    let makeSetRotatorVelocityCmd (axis: RotatorAxis) velocity =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetRotatorVelocity,
            RotatorVelocity = Aris.Command.Types.SetRotatorVelocity(
                Axis = enum (int axis),
                Velocity = velocity
            )
        )

    let makeSetRotatorAccelerationCmd (axis: RotatorAxis) acceleration =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetRotatorAcceleration,
            RotatorAcceleration = Aris.Command.Types.SetRotatorAcceleration(
                Axis = enum (int axis),
                Acceleration = acceleration
            )
        )

    let makeSetRotatorPositionCmd (axis: RotatorAxis) position =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetRotatorPosition,
            RotatorPosition = Aris.Command.Types.SetRotatorPosition(
                Axis = enum (int axis),
                Position = position
            )
        )

    let makeStopRotatorCmd (axis: RotatorAxis) =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.StopRotator,
            RotatorStop = Aris.Command.Types.StopRotator(
                Axis = enum (int axis)
            )
        )

    let makeSalinityCmd (salinity : Salinity) =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetSalinity,
            Salinity = Aris.Command.Types.SetSalinity(
                Salinity = enum (int salinity)
            )
        )

    let pingCommandSingleton =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.Ping,
            Ping = Aris.Command.Types.Ping()
        )


    let makeAcousticSettingsCmd (v: AcousticSettingsVersioned) =

        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetAcoustics,
            Settings =
                Aris.Command.Types.SetAcousticSettings(
                    Cookie = uint32 v.Cookie,
                    FrameRate = float32 v.Settings.FrameRate,
                    SamplesPerBeam = uint32 v.Settings.SampleCount,
                    SampleStartDelay = uint32 v.Settings.SampleStartDelay,
                    CyclePeriod = uint32 v.Settings.CyclePeriod,
                    SamplePeriod = uint32 v.Settings.SamplePeriod,
                    PulseWidth = uint32 v.Settings.PulseWidth,
                    PingMode = v.Settings.PingMode.ToUInt32(),
                    EnableTransmit = v.Settings.EnableTransmit,
                    Frequency = enum (int32 v.Settings.Frequency),
                    Enable150Volts = v.Settings.Enable150Volts,
                    ReceiverGain = float32 v.Settings.ReceiverGain)
        )

