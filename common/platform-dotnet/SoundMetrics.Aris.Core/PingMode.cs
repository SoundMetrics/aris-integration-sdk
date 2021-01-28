// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    public struct PingMode
    {
        private PingMode(
            int integralValue,
            int beamCount,
            int pingsPerFrame)
        {
            this.integralValue = integralValue;
            this.beamCount = beamCount;
            this.pingsPerFrame = pingsPerFrame;
        }

        public int IntegralValue => integralValue;

        public int BeamCount => beamCount;

        public int PingsPerFrame => pingsPerFrame;

        public static readonly PingMode PingMode1 = new PingMode(1, 48, 3);
        public static readonly PingMode PingMode3 = new PingMode(3, 96, 6);
        public static readonly PingMode PingMode6 = new PingMode(6, 64, 4);
        public static readonly PingMode PingMode9 = new PingMode(9, 128, 8);

        public static bool TryGet(int integralValue, out PingMode pingMode)
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

        private readonly int integralValue;
        private readonly int beamCount;
        private readonly int pingsPerFrame;
    }
}
