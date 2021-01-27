// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core.Raw
{
    // This all came from Bill's documents, in the section labeled
    // Calculations for Maximum Frame Rate. See
    // \\soundserv\Engineering\Test\Results & Reports\ARIS Max Frame Rates
    internal static class MaxFrameRate
    {
        internal static Rate FindMaximumFrameRate(
            SystemType systemType,
            PingMode pingMode,
            int samplesPerBeam,
            FineDuration sampleStartDelay,
            FineDuration samplePeriod,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay)
        {
            var cfg = SystemConfiguration.GetConfiguration(systemType);

            // Aliases to match bill's doc; the function interface shouldn't use these.

            var ssd = sampleStartDelay;
            var spb = samplesPerBeam;
            var sp = samplePeriod;
            var ppf = pingMode.PingsPerFrame;
            var aa = antiAliasing;
            var nob = pingMode.BeamCount;

            // from the document

            var mcp = ssd + (sp * spb) + CyclePeriodMargin;

            var cpaFactor =
                DetermineCyclePeriodAdjustmentFactor(systemType, sp);
            var cpa = mcp * cpaFactor;
            var cpa1 = cpa + aa;

            var mfp = interpacketDelay.Enable
                ? CalculateMinimumFramePeriodWithDelay()
                : CalculateMinimumFramePeriod();

            // De-alias
            var maxFramePeriod = mfp;

            var maximumFrameRate = 1 / maxFramePeriod;
            var trueMin = cfg.FrameRateRange.Minimum;
            var trueMax = cfg.FrameRateRange.Maximum;
            var limitedRate = Rate.Max(trueMin, Rate.Min(maximumFrameRate, trueMax));

            return limitedRate;

            FineDuration CalculateMinimumFramePeriod() =>
                    ppf * (mcp + cpa1);

            FineDuration CalculateMinimumFramePeriodWithDelay()
            {
                var id = interpacketDelay.Delay;
                var headroom = FineDuration.FromMicroseconds(16.6);

                return
                    ppf * (mcp + cpa1)
                        + (((nob * spb) + 1024) / 1392) * (headroom + id);
            }
        }

        private static double DetermineCyclePeriodAdjustmentFactor(
            SystemType systemType,
            FineDuration samplePeriod)
        {
            var isSmallSamplePeriod = samplePeriod <= FineDuration.FromMicroseconds(4);

            if (systemType == SystemType.Aris3000)
            {
                return isSmallSamplePeriod ? 0.18 : 0.03;
            }
            else if (systemType == SystemType.Aris1800)
            {
                return isSmallSamplePeriod ? 0.18 : 0.03;
            }
            else if (systemType == SystemType.Aris1200)
            {
                return 0.02;
            }
            else
            {
                throw new Exception("Unexpected systemtype: use and instance from SystemType statics");
            }
        }

        private static readonly FineDuration CyclePeriodMargin = SystemConfigurationRaw.CyclePeriodMargin;
    }
}
