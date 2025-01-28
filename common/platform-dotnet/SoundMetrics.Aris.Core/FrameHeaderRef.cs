namespace SoundMetrics.Aris.Core
{
    /// <summary>
    /// Read-only reference-based container for FrameHeader for ease of
    /// closing over in lambda expressions, etc.
    /// </summary>
    public sealed class FrameHeaderRef
    {
        public FrameHeaderRef(in FrameHeader frameHeader)
        {
            this.frameHeader = frameHeader;
        }

        public ref readonly FrameHeader Value => ref frameHeader;

        private readonly FrameHeader frameHeader;
    }
}
