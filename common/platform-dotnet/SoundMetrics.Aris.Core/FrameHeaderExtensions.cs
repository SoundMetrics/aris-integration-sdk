using System;

namespace SoundMetrics.Aris.Core
{
    public static class FrameHeaderExtensions
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        public static DateTime SonarOffsetToDateTime(ulong offset)
        {
            var ticks = (long)offset * 10L;
            return new DateTime(Epoch.Ticks + ticks);
        }
    }
}
