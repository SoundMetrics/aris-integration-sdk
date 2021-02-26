// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core.Raw
{
    // This all came from Bill's documents, in the section labeled
    // Calculations for Maximum Frame Rate. See
    // \\soundserv\Engineering\Test\Results & Reports\ARIS Max Frame Rates
    public static class MaxFrameRate
    {
        public static Rate DetermineMaximumFrameRate(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            return DetermineMaximumFrameRate(
                settings.SystemType.GetConfiguration(),
                settings.PingMode,
                settings.SampleCount,
                settings.SampleStartDelay,
                settings.SamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay);
        }

        public static Rate DetermineMaximumFrameRate(
            AcousticSettingsRaw settings,
            out FineDuration cyclePeriod)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            return DetermineMaximumFrameRate(
                settings.SystemType.GetConfiguration(),
                settings.PingMode,
                settings.SampleCount,
                settings.SampleStartDelay,
                settings.SamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                out cyclePeriod);
        }

        internal static Rate DetermineMaximumFrameRate(
            SystemConfiguration sysCfg,
            PingMode pingMode,
            int sampleCount,
            FineDuration sampleStartDelay,
            FineDuration samplePeriod,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay)
        {
            return
                DetermineMaximumFrameRate(
                    sysCfg,
                    pingMode,
                    sampleCount,
                    sampleStartDelay,
                    samplePeriod,
                    antiAliasing,
                    interpacketDelay,
                    out var _);
        }

        // This variant allows us to return the value for cyclePeriod, which is required for
        // sending raw settings to ARIS.
        internal static Rate DetermineMaximumFrameRate(
            SystemConfiguration sysCfg,
            PingMode pingMode,
            int sampleCount,
            FineDuration sampleStartDelay,
            FineDuration samplePeriod,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay,
            out FineDuration cyclePeriod)
        {
            // Aliases to match bill's doc; the function interface shouldn't use these.

            var ssd = sampleStartDelay;
            var sc = sampleCount;
            var sp = samplePeriod;
            var ppf = pingMode.PingsPerFrame;
            var aa = antiAliasing;
            var nob = pingMode.BeamCount;

            // from the document

            var mcp = ssd + (sp * sc) + CyclePeriodMargin;

            var cpaFactor =
                DetermineCyclePeriodAdjustmentFactor(sp, sysCfg);
            var cpa = mcp * cpaFactor;
            var cpa1 = cpa + aa;

            cyclePeriod = mcp + cpa1;

            var mfp = interpacketDelay.Enable
                ? CalculateMinimumFramePeriodWithDelay()
                : CalculateMinimumFramePeriod();

            // De-alias
            var maxFramePeriod = mfp;

            var maximumFrameRate = 1 / maxFramePeriod;
            var limitedRate = maximumFrameRate.ConstrainTo(sysCfg.FrameRateRange);

            return limitedRate.NormalizeToHertz();

            FineDuration CalculateMinimumFramePeriod() =>
                    ppf * (mcp + cpa1);

            FineDuration CalculateMinimumFramePeriodWithDelay()
            {
                var id = interpacketDelay.Delay;
                var headroom = FineDuration.FromMicroseconds(16.6);

                return
                    ppf * (mcp + cpa1)
                        + (((nob * sc) + 1024) / 1392) * (headroom + id);
            }
        }

        private static double DetermineCyclePeriodAdjustmentFactor(
            FineDuration samplePeriod,
            SystemConfiguration sysCfg)
        {
            var isSmallSamplePeriod = samplePeriod <= FineDuration.FromMicroseconds(4);
            return isSmallSamplePeriod ? sysCfg.SmallPeriodAdjustmentFactor : sysCfg.LargePeriodAdjustmentFactor;
        }

        private static readonly FineDuration CyclePeriodMargin = SystemConfigurationRaw.CyclePeriodMargin;
    }
}
