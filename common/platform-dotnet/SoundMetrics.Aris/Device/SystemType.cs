using System;

namespace SoundMetrics.Aris.Device
{
    /// Defines the system types for ARIS: 1200, 1800, and 3000.
    public enum SystemType
    {
        Aris1800 = 0,
        Aris3000 = 1,
        Aris1200 = 2,
    };

    public static class SystemTypeExtensions
    {
        public static bool IsValid(this SystemType systemType)
        {
            switch (systemType)
            {
                case SystemType.Aris1200:
                case SystemType.Aris1800:
                case SystemType.Aris3000:
                    return true;

                default:
                    return false;
            }
        }

        public static void AssertValid(this SystemType systemType)
        {
            if (!IsValid(systemType))
            {
                throw new ArgumentOutOfRangeException(nameof(systemType));
            }
        }
    }
}
