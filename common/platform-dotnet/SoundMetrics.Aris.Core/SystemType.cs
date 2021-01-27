// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core
{
    /// <summary>
    /// Identifies a system type, the physical configuration of the device.
    /// System types span across ARIS models, so an Explorer, a Defender, and
    /// a Voyager may all be an ARIS 3000.
    /// </summary>
    public sealed class SystemType
    {
        public static readonly SystemType Aris1200 = new SystemType(2, "ARIS 1200");
        public static readonly SystemType Aris1800 = new SystemType(0, "ARIS 1800");
        public static readonly SystemType Aris3000 = new SystemType(1, "ARIS 3000");

        public static bool TryGetFromIntegralValue(int integralValue, out SystemType systemType)
        {
            if (integralValue == Aris1200.IntegralValue)
            {
                systemType = Aris1200;
                return true;
            }
            else if (integralValue == Aris1800.IntegralValue)
            {
                systemType = Aris1800;
                return true;
            }
            else if (integralValue == Aris3000.IntegralValue)
            {
                systemType = Aris1800;
                return true;
            }
            else
            {
                systemType = default;
                return false;
            }
        }

        internal SystemType(int integralValue, string humanReadableString)
        {
            this.integralValue = integralValue;
            this.humanReadableString = humanReadableString;
        }

        internal int IntegralValue => integralValue;

        public string HumanReadableString => humanReadableString;

        private readonly int integralValue;
        private readonly string humanReadableString;
    }
    }
