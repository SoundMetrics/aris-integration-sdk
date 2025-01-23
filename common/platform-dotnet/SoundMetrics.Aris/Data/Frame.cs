using SoundMetrics.Aris.Core;
using System;

namespace SoundMetrics.Aris.Data
{
    public sealed class Frame
    {
        public static bool TryCreate(in FrameHeader frameHeader, ReadOnlySampleBuffer samples, out Frame? frame)
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

                frame = new Frame(frameHeader, sampleGeometry, samples);
                return true;
            }
            else
            {
                // Invalid frame header, couldn't determine frame geometry
                frame = default;
                return false;
            }
        }

        private Frame(
            in FrameHeader frameHeader,
            in SampleGeometry sampleGeometry,
            ReadOnlySampleBuffer samples)
        {
            FrameHeader = frameHeader;
            SampleGeometry = sampleGeometry;
            Samples = samples;
        }

        public FrameHeader FrameHeader { get; private set; }

        public ReadOnlySampleBuffer Samples { get; private set; }

        public SampleGeometry SampleGeometry { get; private set; }
    }
}
