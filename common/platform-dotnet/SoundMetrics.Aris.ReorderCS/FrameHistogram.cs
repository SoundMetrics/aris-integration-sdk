// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

using System.Diagnostics;

namespace SoundMetrics.Aris.ReorderCS
{
    /// <summary>
    /// A histogram of sample value counts.
    /// </summary>
    public class FrameHistogram
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
        /// <returns>A histogram.</returns>
        public static unsafe FrameHistogram Generate(byte* samples, int sampleCount)
        {
            var counts = new int[256];
            byte* p = samples;
            for (int i = 0; i < sampleCount; ++i, ++p)
            {
                counts[*p] += 1;
            }

            return new FrameHistogram(counts);
        }

        /// <summary>
        /// Generates an initialized instance of the type.
        /// </summary>
        /// <param name="samples">Samples to be read.</param>
        /// <returns>A histogram.</returns>
        public static FrameHistogram Generate(byte[] samples)
        {
            var sampleCount = samples.Length;
            var counts = new int[256];

            for (int i = 0; i < sampleCount; ++i)
            {
                counts[samples[i]] += 1;
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
