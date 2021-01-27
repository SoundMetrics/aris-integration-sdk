using SoundMetrics.Aris.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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
                throw new ArgumentException("Invalid frame header provided: sample geometry");
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

            var samples = new ByteBuffer(sampleParts);

            return Frame.TryCreate(frameHeader, samples, out var newFrame)
                        && !(newFrame is null)
                        && FrameSampleOrder.TryReorderFrame(newFrame, out frame);
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
