// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

using System.Diagnostics;

namespace SoundMetrics.Aris.ReorderCS
{
    /// <summary>
    /// A histogram of sample value counts.
    /// </summary>
    public struct FrameHistogram
    {
        /// <summary>
        /// The counts.
        /// </summary>
        public readonly int[] Counts;

        /// <summary>
        /// Generates an initialized instance of the type.
        /// </summary>
        /// <param name="samples">Samples to be read.</param>
        /// <param name="sampleCount">Number of samples to be read.</param>
        /// <returns></returns>
        public static unsafe FrameHistogram Generate(byte* samples, uint sampleCount)
        {
            var counts = new int[256];
            byte* p = samples;
            for (uint i = 0; i < sampleCount; ++i, ++p)
            {
                counts[*p] += 1;
            }

            return new FrameHistogram(counts);
        }

        private FrameHistogram(int[] counts)
        {
            Debug.Assert(counts.Length == 256);
            Counts = counts;
        }
    }
}
