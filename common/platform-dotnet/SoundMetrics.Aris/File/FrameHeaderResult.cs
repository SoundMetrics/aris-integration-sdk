using SoundMetrics.Aris.Data;

namespace SoundMetrics.Aris.File
{
    public struct FrameHeaderResult
    {
        public Box<ArisFrameHeader> FrameHeader { get; private set; }
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }

        public static FrameHeaderResult FromFrameHeader(in ArisFrameHeader frameHeader)
        {
            return new FrameHeaderResult
            {
                Success = true,
                FrameHeader = new Box<ArisFrameHeader>(frameHeader),
                ErrorMessage = "",
            };
        }

        public static FrameHeaderResult FromError(string errorMessage)
        {
            return new FrameHeaderResult
            {
                Success = false,
                ErrorMessage = errorMessage ?? "",
            };
        }
    }
}
