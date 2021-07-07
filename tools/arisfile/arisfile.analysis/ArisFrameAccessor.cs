using SoundMetrics.Aris.Headers;
using System;

namespace arisfile.analysis
{
    public static class ArisFrameHeaderExtensions
    {
        public static DateTime GetTopsideTimestamp(this in ArisFrameHeader frameHeader) =>
            MakeTimestamp(frameHeader.FrameTime);

        public static DateTime GetArisTimestamp(this in ArisFrameHeader frameHeader) =>
            MakeTimestamp(frameHeader.sonarTimeStamp);

        private static DateTime MakeTimestamp(UInt64 microsecondsSinceEpoch)
        {
            var millisecondOffset = microsecondsSinceEpoch / 1000.0;
            return new DateTime(1970, 1, 1).AddMilliseconds(millisecondOffset);
        }
    }

    public struct ArisFrameAccessor
    {
        public int CalculatedFrameIndex;
        public ArisFrameHeader ArisFrameHeader;
    }
}
