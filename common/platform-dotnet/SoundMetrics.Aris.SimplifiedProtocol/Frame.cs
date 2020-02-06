using SoundMetrics.Aris.Headers;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    public class Frame
    {
        public ArisFrameHeader Header;
        public NativeBufferHandle Samples;
    }
}
