using SoundMetrics.Aris.Headers;
using System;

namespace SoundMetrics.Aris.Data
{
    public static class ArisFrameHeaderExtensions
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        public static DateTime GetSonarTimestamp(this in ArisFrameHeader frameHeader)
        {
            var ticks = (long)frameHeader.sonarTimeStamp * 10L;
            return new DateTime(Epoch.Ticks + ticks);
        }
    }
}
