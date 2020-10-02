namespace SoundMetrics.Aris.Network
{
    public struct FrameListenerMetrics
    {
        public long FramesStarted;
        public long FramesCompleted;
        public long PacketsReceived;
        public long InvalidPacketsReceived;

        public static FrameListenerMetrics operator +(FrameListenerMetrics a, FrameListenerMetrics b)
        {
            return new FrameListenerMetrics
            {
                FramesStarted = a.FramesStarted + b.FramesStarted,
                FramesCompleted = a.FramesCompleted + b.FramesCompleted,
                PacketsReceived = a.PacketsReceived + b.PacketsReceived,
                InvalidPacketsReceived = a.InvalidPacketsReceived + b.InvalidPacketsReceived,
            };
        }
    }
}
