using SoundMetrics.Aris.Data;

namespace SoundMetrics.Aris.File
{
    public struct FrameResult
    {
        public Box<ArisFrameHeader> FrameHeader { get; private set; }
        public ByteBuffer Samples { get; private set; }
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }

        public static FrameResult FromFrame(
            in ArisFrameHeader frameHeader,
            ByteBuffer samples)
        {
            return new FrameResult
            {
                Success = true,
                FrameHeader = new Box<ArisFrameHeader>(frameHeader),
                Samples = samples,
                ErrorMessage = "",
            };
        }

        public static FrameResult FromError(string errorMessage)
        {
            return new FrameResult
            {
                Success = false,
                ErrorMessage = errorMessage ?? "",
            };
        }
    }
}
