// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    // Constraint implementations.
    // These methods spell out their inputs to allow for dependency analysis.
    // `SystemConfiguration` is considered ambient context, is immutable, and
    // is not used in dependency analysis.
    //
    // For example, "what ping modes are available for this ARIS?" uses a
    // SystemConfiguration value; however, "what is the current ping mode"
    // refers to a user input.
    internal static class AcousticSettingsConstraints
    {
        internal static Rate ConstrainFrameRate(
            Rate requestedFrameRate,
            SystemConfiguration sysCfg,
            PingMode pingMode,
            int sampleCount,
            FineDuration sampleStartDelay,
            FineDuration samplePeriod,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay)
        {
            var min = sysCfg.FrameRateRange.Minimum;
            var max = MaxFrameRate.DetermineMaximumFrameRate(
                        sysCfg,
                        pingMode,
                        sampleCount,
                        sampleStartDelay,
                        samplePeriod,
                        antiAliasing,
                        interpacketDelay);

            var goodRange = new ValueRange<Rate>(min, max);
            return requestedFrameRate.ConstrainTo(goodRange);
        }


        internal static FineDuration ConstrainAntiAliasing(this AcousticSettingsRaw settings)
            => settings
                    .SystemType.GetConfiguration()
                    .RawConfiguration
                    .MaxAntialiasingFor(settings);
    }
}
