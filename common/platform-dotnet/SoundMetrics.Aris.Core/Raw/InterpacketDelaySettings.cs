// Copyright (c) 2010-2021 Sound Metrics Corp.

using SoundMetrics.Aris.Core.ApprovalTests;
using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core.Raw
{
    [DebuggerDisplay("{DisplayString}")]
    public struct InterpacketDelaySettings
        : IEquatable<InterpacketDelaySettings>, IPrettyPrintable
    {
#pragma warning disable CA1051 // Do not declare visible instance fields
        public bool Enable;
        public FineDuration Delay;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public string DisplayString =>
            $"{Delay.TotalMicroseconds} \u00B5s" + (Enable ? "" : " disabled");

        public static readonly InterpacketDelaySettings Off = new InterpacketDelaySettings { Enable = false };

        public override bool Equals(object? obj)
            => (obj is InterpacketDelaySettings) ? Equals((InterpacketDelaySettings)obj) : false;

        public bool Equals(InterpacketDelaySettings other)
            => this.Enable == other.Enable && this.Delay == other.Delay;

        public static bool operator ==(InterpacketDelaySettings a, InterpacketDelaySettings b)
            => a.Equals(b);
        public static bool operator !=(InterpacketDelaySettings a, InterpacketDelaySettings b)
            => !a.Equals(b);

        public override string ToString() => $"(Enable={Enable}; Delay={Delay})";

        public override int GetHashCode()
        {
            return Enable.GetHashCode() ^ Delay.GetHashCode();
        }

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
