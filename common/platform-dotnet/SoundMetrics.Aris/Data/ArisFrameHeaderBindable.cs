using SoundMetrics.Aris.Headers;

namespace SoundMetrics.Aris.Data
{
    public sealed class ArisFrameHeaderBindable
    {
        public ArisFrameHeaderBindable(in ArisFrameHeader header)
        {
            this.header = header;
        }

        private readonly ArisFrameHeader header;

        /// Gets the entire ArisFrameHeader.
        public ArisFrameHeader EntireHeader => header;

        public int BeamCount =>
            Device.SonarConfig.GetPingModeDefinition(Device.PingMode.From((int)header.PingMode))
                .ChannelCount;
    }
}
