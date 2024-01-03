using SoundMetrics.Aris.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SoundMetrics.Aris.Data
{
    internal sealed class FrameAssembler
    {
        public void SetFrameHeader(in FrameHeader frameHeader)
        {
            Reset();
            this.frameHeader = frameHeader;
            frameIndex = frameHeader.FrameIndex;

            if (SystemConfiguration.TryGetSampleGeometry(frameHeader, out var sampleGeometry))
            {
                expectedSampleCount = sampleGeometry.TotalSampleCount;
            }
            else
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Invalid frame header provided: sample geometry");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
        }

        public bool AddFramePart(uint framePart, ReadOnlyMemory<byte> samples)
        {
            if (framePart == expectedFramePart++)
            {
                accumulatedSamples += samples.Length;
                sampleParts.Add(samples);
                return accumulatedSamples == expectedSampleCount;
            }
            else
            {
                return false;
            }
        }

        public bool GetFullFrame(out Frame? frame)
        {
            frame = default;

            return frameHeader is FrameHeader fh
                && accumulatedSamples == expectedSampleCount
                && TryConstructFrame(fh, sampleParts, out frame);
        }

        private void Reset()
        {
            expectedFramePart = 0;
            accumulatedSamples = 0;
            frameHeader = default;
            sampleParts.Clear();
        }

        private static bool TryConstructFrame(
            in FrameHeader frameHeader,
            List<ReadOnlyMemory<byte>> sampleParts,
            out Frame? frame)
        {
            frame = default;

#pragma warning disable CA2000 // Dispose objects before losing scope
            // Ownership of `samples` is given away.
            var samples = ByteBuffer.Create(sampleParts);
#pragma warning restore CA2000 // Dispose objects before losing scope
            try
            {
                return Frame.TryCreate(frameHeader, samples, out var newFrame)
                            && !(newFrame is null)
                            && FrameSampleOrder.TryReorderFrame(newFrame, out frame);
            }
            catch
            {
                samples.Dispose();
                throw;
            }
        }

        private FrameHeader? frameHeader;
        private uint expectedFramePart;
        private int expectedSampleCount;
        private int accumulatedSamples;
        private uint frameIndex;
        private List<ReadOnlyMemory<byte>> sampleParts =
            new List<ReadOnlyMemory<byte>>();
    }
}
