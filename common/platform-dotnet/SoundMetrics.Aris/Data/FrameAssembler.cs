using SoundMetrics.Aris.Device;
using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Data
{
    internal sealed class FrameAssembler
    {
        public void SetFrameHeader(in FrameHeader frameHeader)
        {
            Reset();
            this.frameHeader = frameHeader;
            frameIndex = frameHeader.FrameIndex;

            var (_, _, totalSampleCount, _) = SonarConfig.GetSampleGeometry(frameHeader);
            expectedSampleCount = totalSampleCount;
        }

        public bool AddFramePart(uint framePart, Memory<byte> samples)
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

        public bool GetFullFrame(out Frame frame)
        {
            if (frameHeader is FrameHeader fh
                && accumulatedSamples == expectedSampleCount)
            {
                frame = ConstructFrame(fh, sampleParts);
                return true;
            }
            else
            {
                frame = default;
                return false;
            }
        }

        private void Reset()
        {
            expectedFramePart = 0;
            accumulatedSamples = 0;
            frameHeader = default;
            sampleParts.Clear();
        }

        private static Frame ConstructFrame(
            in FrameHeader frameHeader,
            IEnumerable<Memory<byte>> sampleParts)
        {
            // REORDER
            // TODO
            throw new NotImplementedException();
        }

        private FrameHeader? frameHeader;
        private List<Memory<byte>> sampleParts = new List<Memory<byte>>();
        private uint expectedFramePart;
        private int expectedSampleCount;
        private int accumulatedSamples;
        private uint frameIndex;
    }
}
