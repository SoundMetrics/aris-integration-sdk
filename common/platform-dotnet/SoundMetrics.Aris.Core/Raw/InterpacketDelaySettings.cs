// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core.Raw
{
    [DebuggerDisplay("{DisplayString}")]
    public struct InterpacketDelaySettings : IEquatable<InterpacketDelaySettings>
    {
        public bool Enable;
        public FineDuration Delay;

        public string DisplayString =>
            $"{Delay.TotalMicroseconds} \u00B5s" + (Enable ? "" : " disabled");

        public override bool Equals(object obj)
            => (obj is InterpacketDelaySettings) ? Equals((InterpacketDelaySettings)obj) : false;

        public bool Equals(InterpacketDelaySettings other)
            => this.Enable == other.Enable && this.Delay == other.Delay;

        public static bool operator ==(InterpacketDelaySettings a, InterpacketDelaySettings b)
            => a.Equals(b);
        public static bool operator !=(InterpacketDelaySettings a, InterpacketDelaySettings b)
            => !a.Equals(b);

        public override int GetHashCode()
        {
            return Enable.GetHashCode() ^ Delay.GetHashCode();
        }
    }
}
