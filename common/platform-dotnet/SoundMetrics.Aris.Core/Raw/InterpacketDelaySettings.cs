// Copyright (c) 2010-2025 Sound Metrics Corp.

using SoundMetrics.Aris.Core.ApprovalTests;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core.Raw
{
    [DebuggerDisplay("{DisplayString}")]
    public record struct InterpacketDelaySettings(
        bool Enable,
        FineDuration Delay)
        : IPrettyPrintable
    {

        public readonly string DisplayString =>
            $"{Delay.TotalMicroseconds} \u00B5s" + (Enable ? "" : " disabled");

        public static readonly InterpacketDelaySettings Off = new InterpacketDelaySettings { Enable = false };

        public override string ToString() => $"(Enable={Enable}; Delay={Delay})";

        PrettyPrintHelper IPrettyPrintable.PrettyPrint(
            PrettyPrintHelper helper,
            string label)
        {
            helper.PrintHeading($"{label}: {nameof(InterpacketDelaySettings)}");

            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("Enable", Enable);
                helper.PrintValue("Delay", Delay);
            }

            return helper;
        }
    }
}
