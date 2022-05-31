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

            return DetermineMaximumFrameRate(settings, out var _);
        }

        public static Rate DetermineMaximumFrameRate(
            AcousticSettingsRaw settings,
            out FineDuration cyclePeriod)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return DetermineMaximumFrameRateWithIntermediates(
                settings.SystemType.GetConfiguration(),
                settings.PingMode,
                settings.SampleCount,
                settings.SampleStartDelay,
                settings.SamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                out cyclePeriod,
                out var _);
        }

        internal static Rate DetermineMaximumFrameRateWithIntermediates(
            AcousticSettingsRaw settings,
            out FineDuration cyclePeriod,
            out IntermediateMaximumFrameRateResults intermediateResults)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return DetermineMaximumFrameRateWithIntermediates(
                settings.SystemType.GetConfiguration(),
                settings.PingMode,
                settings.SampleCount,
                settings.SampleStartDelay,
                settings.SamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                out cyclePeriod,
                out intermediateResults);
        }

        internal struct IntermediateMaximumFrameRateResults
        {
            public FineDuration MCP;
            public FineDuration CPA1;
            public int PPF;

            public override string ToString() => $"MCP=[{MCP}]; PPF=[{PPF}]";
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
                DetermineMaximumFrameRateWithIntermediates(
                    sysCfg,
                    pingMode,
                    sampleCount,
                    sampleStartDelay,
                    samplePeriod,
                    antiAliasing,
                    interpacketDelay,
                    out var _,
                    out var _);
        }

        // This variant allows us to return the value for cyclePeriod, which is required for
        // sending raw settings to ARIS.
        internal static Rate DetermineMaximumFrameRateWithIntermediates(
            SystemConfiguration sysCfg,
            PingMode pingMode,
            int sampleCount,
            FineDuration sampleStartDelay,
            FineDuration samplePeriod,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelaySettings,
            out FineDuration cyclePeriod,
            out IntermediateMaximumFrameRateResults intermediateResults)
        {
            // Aliases to match Bill's doc, only for reference:
            // \\soundserv\Software\ARIS\ARIS Documentation\Sonar\ARIS Maximum Frame Rate Calculation.docx
            // The function interface shouldn't use these abbreviated names as
            // they are not good names for an API.

            var ssd = sampleStartDelay;
            var spb = sampleCount;
            var sp = samplePeriod;
            var ppf = pingMode.PingsPerFrame;
            var aa = antiAliasing;
            var nob = pingMode.BeamCount;

            // From Bill's document

            var mcp = ssd + (sp * spb) + CyclePeriodMargin;

            var cpaFactor = DetermineCyclePeriodAdjustmentFactor(sp, sysCfg);
            var cpa = mcp * cpaFactor;
            var cpa1 = cpa + aa;

            cyclePeriod = mcp + cpa1;

            var mfp = interpacketDelaySettings.Enable
                ? CalculateMinimumFramePeriodWithInterpacketDelay()
                : CalculateMinimumFramePeriod();

            // Back to good naming
            var maxFramePeriod = mfp;

            var maximumFrameRate = 1 / maxFramePeriod;
            var limitedRate = maximumFrameRate.ConstrainTo(sysCfg.FrameRateRange);
            var hz = limitedRate.NormalizeToHertz();

            intermediateResults =
                new IntermediateMaximumFrameRateResults
                {
                    MCP = mcp,
                    CPA1 = cpa1,
                    PPF = ppf,
                };

            return hz;


            FineDuration CalculateMinimumFramePeriod() =>
                    ppf * (mcp + cpa1);

            FineDuration CalculateMinimumFramePeriodWithInterpacketDelay()
            {
                var headroom = (FineDuration)16.6;

                return
                    ppf * (mcp + cpa1)
                        + (((nob * spb) + 1024) / 1392) * (headroom + interpacketDelaySettings.Delay);
            }
        }

        private static double DetermineCyclePeriodAdjustmentFactor(
            FineDuration samplePeriod,
            SystemConfiguration sysCfg)
        {
            var isSmallSamplePeriod = samplePeriod <= (FineDuration)4;
            return isSmallSamplePeriod ? sysCfg.SmallPeriodAdjustmentFactor : sysCfg.LargePeriodAdjustmentFactor;
        }

        private static readonly FineDuration CyclePeriodMargin = SystemConfigurationRaw.CyclePeriodMargin;
    }
}
