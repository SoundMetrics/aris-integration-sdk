using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Core
{
    public static class PingModeExtensions
    {
        internal static PingModeInfo GetInfo(this PingMode pingMode)
        {
            return extraInfos[pingMode.IntegralValue];
        }

        internal struct PingModeInfo : IEquatable<PingModeInfo>
        {
            public int BeamCount;
            public int PingsPerFrame;

            public override bool Equals(object obj)
                => obj is PingModeInfo other && this.Equals(other);

            public bool Equals(PingModeInfo other)
                => this.BeamCount == other.BeamCount
                    && this.PingsPerFrame == other.PingsPerFrame;

            public static bool operator ==(PingModeInfo a, PingModeInfo b)
                => a.Equals(b);

            public static bool operator !=(PingModeInfo a, PingModeInfo b)
                => !a.Equals(b);

            public override int GetHashCode()
                => BeamCount.GetHashCode() ^ PingsPerFrame.GetHashCode();
        }

        private static readonly Dictionary<int, PingModeInfo> extraInfos =
            new Dictionary<int, PingModeInfo>
            {
                { 1, new PingModeInfo { BeamCount =  48, PingsPerFrame = 3 } },
                { 3, new PingModeInfo { BeamCount =  96, PingsPerFrame = 6 } },
                { 6, new PingModeInfo { BeamCount =  64, PingsPerFrame = 4 } },
                { 9, new PingModeInfo { BeamCount = 128, PingsPerFrame = 8 } },
            };
    }
}
