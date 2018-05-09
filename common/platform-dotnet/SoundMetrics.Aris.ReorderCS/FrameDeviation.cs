// Copyright 2018 Sound Metrics Corp. All Rights Reserved.

using System;

namespace SoundMetrics.Aris.ReorderCS
{
    internal static class FrameDeviation
    {
        /// <summary>
        /// Calculates the population standard deviation of a frame.
        /// </summary>
        /// <param name="samples">The samples examined.</param>
        /// <param name="sampleCount">The number of samples.</param>
        /// <returns>Population standard deviation.</returns>
        public static unsafe double FrameStdDev(byte* samples, int sampleCount)
        {
            // This is a population standard deviation per
            // https://en.wikipedia.org/wiki/Standard_deviation#Population_standard_deviation_of_grades_of_eight_students

            int sum = SumAllSamples(samples, sampleCount);
            int mean = sum / sampleCount;

            long variance = Variance(samples, sampleCount, mean);
            double stdDev = Math.Sqrt(variance);

            return stdDev;
        }

        internal static unsafe int SumAllSamples(byte* samples, int sampleCount)
        {
            // Find the average. Max samples for ARIS is approx 128 * 4096, or
            // 2^7 * 2^12, or 2^19 (half a MiB). The max sample value is 2^8 - 1.
            // An accumulator to sum all samples needs to accommodate 2^19 * 255,
            // or roughly 2^19 * 2^8, so 2^27. A 32-bit integer will do.

            int sum = 0;
            byte* p = samples;
            for (int i = 0; i < sampleCount; ++i, ++p)
            {
                sum += *p;
            }

            return sum;
        }

        internal static unsafe long Variance(byte* samples, int sampleCount, int mean)
        {
            // Max square for each sample is 255^2, or roughly 2^16. Max sample count
            // is 2^19, so max sum of deviation squared is 2^16 * 2^19 or 2^35. So a
            // 64-bit accumulator is need here.

            long sum = 0;
            byte* p = samples;
            for (int i = 0; i < sampleCount; ++i, ++p)
            {
                int deviation = (int)*p - mean;
                sum += deviation * deviation;
            }

            return sum / sampleCount;
        }
    }
}
