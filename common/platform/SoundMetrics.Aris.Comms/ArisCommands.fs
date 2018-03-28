// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

module ArisCommands =
    open System
    open System.Net

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

        FocusMap.mapFocusRangeToFocusUnits systemType range degreesC salinity telephoto

    let makeFocusCmd (requestedFocus: FocusUnits) =
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

    let makeSalinityCmd salinity =
        Aris.Command(
            Type = Aris.Command.Types.CommandType.SetSalinity,
            Salinity = Aris.Command.Types.SetSalinity(
                Salinity = enum salinity
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
                    Cookie = uint32 v.cookie,
                    FrameRate = float32 v.settings.FrameRate,
                    SamplesPerBeam = uint32 v.settings.SampleCount,
                    SampleStartDelay = uint32 v.settings.SampleStartDelay,
                    CyclePeriod = uint32 v.settings.CyclePeriod,
                    SamplePeriod = uint32 v.settings.SamplePeriod,
                    PulseWidth = uint32 v.settings.PulseWidth,
                    PingMode = uint32 v.settings.PingMode,
                    EnableTransmit = v.settings.EnableTransmit,
                    Frequency = enum (int32 v.settings.Frequency),
                    Enable150Volts = v.settings.Enable150Volts,
                    ReceiverGain = float32 v.settings.ReceiverGain)
        )

