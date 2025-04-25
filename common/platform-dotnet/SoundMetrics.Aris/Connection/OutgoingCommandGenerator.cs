using Google.Protobuf;
using SoundMetrics.Aris.Connection.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using static Aris.Command.Types;

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

    private static List<byte[]> Generate(ApplySettingsRequest settingsRequest)
    {
        var settings = settingsRequest.Settings;

        SetAcousticSettings message = new()
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

        return GenerateMessageParts(message);
    }

    private static List<byte[]> Generate(InitializeDeviceConnection initializeCommand)
    {
        SetSalinity setSalinity = new()
        {
            Salinity = (SetSalinity.Types.Salinity)initializeCommand.Salinity,
        };

        SetFrameStreamReceiver setFrameStreamReceiver = new()
        {
            Ip = GetIPv4AddressString(initializeCommand.ReceiverEndPoint),
            Port = (uint)initializeCommand.ReceiverEndPoint.Port,
        };

        SetDateTime setDateTime = new()
        {
            DateTime = FormatTimestamp(initializeCommand.Timestamp),
        };

        return GenerateMessageParts(setSalinity, setFrameStreamReceiver, setDateTime);
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

    private static string FormatTimestamp(DateTimeOffset timestamp) =>
        $"{timestamp.Year}-{MonthAbbreviations[timestamp.Month - 1]}-{timestamp.Day:D02} "
        + $"{timestamp.Hour:D02}:{timestamp.Minute:D02}:{timestamp.Second:D02}";
}
