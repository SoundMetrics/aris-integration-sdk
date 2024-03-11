// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Text.Json.Serialization;

namespace SoundMetrics.Aris.Core
{
    [JsonConverter(typeof(PingModeJsonConverter))]
    public struct PingMode : IEquatable<PingMode>
    {
        private PingMode(int integralValue, int beamCount, int pingsPerFrame)
        {
            this.integralValue = integralValue;
            this.beamCount = beamCount;
            this.pingsPerFrame = pingsPerFrame;
        }

        public int IntegralValue => integralValue;

        public int BeamCount => beamCount;

        public int PingsPerFrame => pingsPerFrame;

        public static readonly PingMode PingMode1 = new PingMode(1, beamCount: 48, pingsPerFrame: 3);
        public static readonly PingMode PingMode3 = new PingMode(3, beamCount: 96, pingsPerFrame: 6);
        public static readonly PingMode PingMode6 = new PingMode(6, beamCount: 64, pingsPerFrame: 4);
        public static readonly PingMode PingMode9 = new PingMode(9, beamCount: 128, pingsPerFrame: 8);

        internal static bool TryGet(int integralValue, out PingMode pingMode)
        {
            switch (integralValue)
            {
                case 1:
                    pingMode = PingMode1;
                    break;
                case 3:
                    pingMode = PingMode3;
                    break;
                case 6:
                    pingMode = PingMode6;
                    break;
                case 9:
                    pingMode = PingMode9;
                    break;

                default:
                    pingMode = default;
                    return false;
            }

            return true;
        }

        internal static PingMode GetFrom(int integralValue)
        {
            if (TryGet(integralValue, out var pingMode))
            {
                return pingMode;
            }

            throw new ArgumentOutOfRangeException(nameof(integralValue));
        }

        public override bool Equals(object? obj)
            => (obj is PingMode) ? Equals((PingMode)obj) : false;

        public bool Equals(PingMode other) =>
            this.integralValue == other.integralValue
            && this.beamCount == other.beamCount
            && this.pingsPerFrame == other.pingsPerFrame;

        public static bool operator ==(PingMode a, PingMode b) => a.Equals(b);
        public static bool operator !=(PingMode a, PingMode b) => !a.Equals(b);

        public override string ToString() => $"PingMode {integralValue}, {beamCount} beams, {pingsPerFrame} pings per frame";

        public override int GetHashCode() =>
            integralValue.GetHashCode() ^ beamCount.GetHashCode() ^ pingsPerFrame.GetHashCode();

        private readonly int integralValue;

        private readonly int beamCount;

        private readonly int pingsPerFrame;
    }
}
