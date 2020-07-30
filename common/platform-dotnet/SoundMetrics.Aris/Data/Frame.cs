using SoundMetrics.Aris.Device;
using System;

namespace SoundMetrics.Aris.Data
{
    public class Frame
    {
        public Frame(in FrameHeader frameHeader, ByteBuffer samples)
        {
            if (samples is null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            var (_, _, totalSampleCount, _) = SonarConfig.GetSampleGeometry(frameHeader);
            if (totalSampleCount != samples.Length)
            {
                throw new ArgumentException(
                    $"Sample count doesn't match frame header; expected [{totalSampleCount}], found [{samples.Length}]");
            }

            FrameHeader = frameHeader;
            Samples = samples;
        }

        public FrameHeader FrameHeader { get; private set; }
        public ByteBuffer Samples { get; private set; }
    }
}
