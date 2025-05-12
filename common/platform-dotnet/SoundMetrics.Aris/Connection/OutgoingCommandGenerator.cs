using Google.Protobuf;
using Serilog;
using SoundMetrics.Aris.Connection.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using static Aris.Command.Types;

using aris = global::Aris;


namespace SoundMetrics.Aris.Connection;

internal static class OutgoingCommandGenerator
{
    public static IReadOnlyList<byte[]> GenerateOutgoingCommands(IOutgoingCommand outgoingCommand)
    {
        ArgumentNullException.ThrowIfNull(outgoingCommand);

        return outgoingCommand switch
        {
            ApplySettingsRequest settingsRequest => Generate(settingsRequest),
            InitializeDeviceConnection initializeCommand => Generate(initializeCommand),

            _ => throw new ArgumentException(
                $"Unhandled command type: [{outgoingCommand.GetType().Name}]",
                nameof(outgoingCommand)),
        };
    }

    private static List<byte[]> Generate(InitializeDeviceConnection initializeCommand)
    {
        Log.Debug("Sending Framestream receiver as [{ep}]", initializeCommand.ReceiverEndPoint);

        return GenerateMessageParts(
            MakeSetSalinityCommand(initializeCommand.Salinity),
            MakeSetFrameStreamReceiverCommand(initializeCommand.ReceiverEndPoint),
            MakeSetDateTimeCommand(initializeCommand.Timestamp),
            MakeSettingsRequestCommand(initializeCommand.ApplySettingsRequest)
            );
    }

    private static List<byte[]> Generate(ApplySettingsRequest applySettingsRequest)
    {
        var command = MakeSettingsRequestCommand(applySettingsRequest);
        return GenerateMessageParts(command);
    }

    private static aris.Command MakeSetSalinityCommand(Core.Salinity salinity)
    {
        return new aris.Command
        {
            Type = aris.Command.Types.CommandType.SetSalinity,
            Salinity = new SetSalinity
            {
                Salinity = (SetSalinity.Types.Salinity)salinity,
            },
        };
    }

    private static aris.Command MakeSetFrameStreamReceiverCommand(IPEndPoint receiverEndPoint)
    {
        return new aris.Command
        {
            Type = aris.Command.Types.CommandType.SetFramestreamReceiver,
            FrameStreamReceiver = new SetFrameStreamReceiver
            {
                Ip = GetIPv4AddressString(receiverEndPoint),
                Port = (uint)receiverEndPoint.Port,
            },
        };
    }

    private static aris.Command MakeSetDateTimeCommand(DateTimeOffset dateTime)
    {
        var formattedDateTime = FormatDateTime(dateTime);
        return new aris.Command
        {
            Type = aris.Command.Types.CommandType.SetDatetime,
            DateTime = new SetDateTime { DateTime = formattedDateTime },
        };
    }

    private static aris.Command MakeSettingsRequestCommand(ApplySettingsRequest settingsRequest)
    {
        var settings = settingsRequest.Settings;
        var settingsPart = new SetAcousticSettings()
        {
            Cookie = settingsRequest.SettingsCookie,
            FrameRate = (float)settings.FrameRate.Hz,
            SamplesPerBeam = (uint)settings.SampleCount,
            SampleStartDelay = (uint)settings.SampleStartDelay.TotalMicroseconds,
            CyclePeriod = (uint)settings.CyclePeriod.TotalMicroseconds,
            SamplePeriod = (uint)settings.SamplePeriod.TotalMicroseconds,
            PulseWidth = (uint)settings.PulseWidth.TotalMicroseconds,
            PingMode = (uint)settings.PingMode.IntegralValue,
            EnableTransmit = settings.EnableTransmit,
            Enable150Volts = settings.Enable150Volts,
            ReceiverGain = settings.ReceiverGain,
            Frequency =
            settings.Frequency == SoundMetrics.Aris.Core.Frequency.Low
                    ? SetAcousticSettings.Types.Frequency.Low
                    : SetAcousticSettings.Types.Frequency.High,
        };

        return new aris.Command
        {
            Type = aris.Command.Types.CommandType.SetAcoustics,
            Settings = settingsPart,
        };
    }

    private static List<byte[]> GenerateMessageParts(params IMessage[] messages)
    {
        List<byte[]> messageParts = [];

        foreach (var message in messages)
        {
            byte[] messageContents = message.ToByteArray();
            int messageLength = messageContents.Length;
            var lengthInNetworkOrder = IPAddress.HostToNetworkOrder(messageLength);
            byte[] lengthPrefix = BitConverter.GetBytes(lengthInNetworkOrder);
            messageParts.Add(lengthPrefix);
            messageParts.Add(messageContents);
        }

        return messageParts;
    }

    private static string GetIPv4AddressString(in IPEndPoint ep)
    {
        var address = ep.Address;
        var bytes = address.GetAddressBytes();
        return string.Join(".", bytes.Select(b => (uint)b));
    }


    private static readonly string[] MonthAbbreviations =
        [
            "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        ];

    private static string FormatDateTime(DateTimeOffset dateTime) =>
        dateTime.ToLocalTime().ToString("yyyy'-'MMM'-'dd HH':'mm':'ss",
                                    System.Globalization.CultureInfo.InvariantCulture);
}
