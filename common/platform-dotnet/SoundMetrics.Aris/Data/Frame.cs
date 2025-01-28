using SoundMetrics.Aris.Core;
using System;

namespace SoundMetrics.Aris.Data
{
    public record class Frame(
        FrameHeaderRef FrameHeader,
        SampleGeometry SampleGeometry,
        ReadOnlySampleBuffer Samples)
    {
        public static Frame Create(FrameHeaderRef frameHeader, ReadOnlySampleBuffer samples)
        {
            return Create(in frameHeader.Value, samples);
        }

        public static Frame Create(in FrameHeader frameHeader, ReadOnlySampleBuffer samples)
        {
            ArgumentNullException.ThrowIfNull(samples);

            if (SystemConfiguration.TryGetSampleGeometry(frameHeader, out var sampleGeometry)
                && sampleGeometry is SampleGeometry sg)
            {
                if (sg.TotalSampleCount != samples.Length)
                {
                    throw new ArgumentException(
                        $"Sample count doesn't match frame header; expected [{sg.TotalSampleCount}], found [{samples.Length}]");
                }

                return new Frame(new FrameHeaderRef(frameHeader), sg, samples);
            }
            else
            {
                // Invalid frame header, couldn't determine frame geometry
                var msg = $"Could not grok the sample geometry from the frame header";
                throw new ArgumentException(msg, nameof(frameHeader));
            }
        }
    }
}
