using SoundMetrics.Aris.Core;
using System;

namespace SoundMetrics.Aris.Data
{
    public sealed class Frame
    {
        public static bool TryCreate(in FrameHeader frameHeader, ByteBuffer samples, out Frame? frame)
        {
            if (samples is null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (SystemConfiguration.TryGetSampleGeometry(frameHeader, out var sampleGeometry))
            {
                if (sampleGeometry.TotalSampleCount != samples.Length)
                {
                    throw new ArgumentException(
                        $"Sample count doesn't match frame header; expected [{sampleGeometry.TotalSampleCount}], found [{samples.Length}]");
                }

                frame = new Frame(frameHeader, samples);
                return true;
            }
            else
            {
                // Invalid frame header, couldn't determine frame geometry
                frame = default;
                return false;
            }
        }

        private Frame(in FrameHeader frameHeader, ByteBuffer samples)
        {
            FrameHeader = frameHeader;
            Samples = samples;
        }

        public FrameHeader FrameHeader { get; private set; }
        public ByteBuffer Samples { get; private set; }
    }
}
