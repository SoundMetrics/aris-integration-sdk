using SoundMetrics.Aris.Core;
using System;

namespace SoundMetrics.Aris.Data
{
    public sealed class Frame
    {
        public static Frame Create(in FrameHeader frameHeader, ReadOnlySampleBuffer samples)
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

                return new Frame(frameHeader, sampleGeometry, samples);
            }
            else
            {
                // Invalid frame header, couldn't determine frame geometry
                var msg = $"Could not grok the sample geometry from the frame header";
                throw new ArgumentException(msg, nameof(frameHeader));
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
