using System;

namespace SoundMetrics.Aris.Data
{
    public enum SystemType
    {
        Aris1800 = 0,
        Aris3000 = 1,
        Aris1200 = 2,
    }

    public static class ArisFrameHeaderExtensions
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        public static DateTime SonarOffsetToDateTime(ulong offset)
        {
            var ticks = (long)offset * 10L;
            return new DateTime(Epoch.Ticks + ticks);
        }
    }
}
