// Copyright (c) 2010-2021 Sound Metrics Corp.

using System.Diagnostics;

namespace SoundMetrics.Aris.Core.Raw
{
    [DebuggerDisplay("{DisplayString}")]
    public struct InterpacketDelaySettings
    {
        public bool Enable;
        public FineDuration Delay;

        public string DisplayString =>
            $"{Delay.TotalMicroseconds} \u00B5s" + (Enable ? "" : " disabled");
    }
}
