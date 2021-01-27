using SoundMetrics.Aris.Core;
using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class InitializeCommand : ICommand
    {
        public InitializeCommand(
            DateTimeOffset timestamp,
            int receiverPort,
            Salinity salinity)
        {
            Timestamp = timestamp;
            Salinity = salinity;
            ReceiverPort = receiverPort;
        }

        public DateTimeOffset Timestamp { get; }
        public Salinity Salinity { get; }
        public int ReceiverPort { get; }

        public string[] GenerateCommand() =>
            new[]
            {
                "initialize",
                $"salinity {Salinity.ToString().ToLower()}",
                $"rcvr_port {ReceiverPort}",
                $"datetime {FormatTimestamp(Timestamp)}",
            };

        private static readonly string[] MonthAbbreviations = new[]
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };

        private static string FormatTimestamp(DateTimeOffset timestamp) =>
            $"{timestamp.Year}-{MonthAbbreviations[timestamp.Month - 1]}-{timestamp.Day:D02} "
            + $"{timestamp.Hour:D02}:{timestamp.Minute:D02}:{timestamp.Second:D02}";
    }
}
