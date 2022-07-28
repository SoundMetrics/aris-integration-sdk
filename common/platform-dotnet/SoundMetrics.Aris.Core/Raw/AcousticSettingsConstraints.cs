// Copyright (c) 2010-2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    using System;
    using static FineDuration;

    // Constraint implementations.
    // These methods spell out their inputs to allow for dependency analysis.
    // `SystemConfiguration` is considered ambient context, is immutable, and
    // is not used in dependency analysis.
    //
    // For example, "what ping modes are available for this ARIS?" uses a
    // SystemConfiguration value; however, "what is the current ping mode"
    // refers to a user input.
    public static class AcousticSettingsConstraints
    {
        public static Rate ConstrainFrameRate(
            Rate requestedFrameRate,
            AcousticSettingsRaw acousticSettings)
        {
            if (requestedFrameRate < AcousticSettingsRaw.MinimumFrameRate)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedFrameRate));
            }

            if (acousticSettings is null)
            {
                throw new ArgumentNullException(nameof(acousticSettings));
            }

            return
                AcousticSettingsConstraints.ConstrainFrameRate(
                    requestedFrameRate,
                    acousticSettings.SystemType.GetConfiguration(),
                    acousticSettings.PingMode,
                    acousticSettings.SampleCount,
                    acousticSettings.SampleStartDelay,
                    acousticSettings.SamplePeriod,
                    acousticSettings.AntiAliasing,
                    acousticSettings.InterpacketDelay);
        }


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
            var min = sysCfg.FrameRateLimits.Minimum;
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
            => Min(
                settings.AntiAliasing,
                settings
                    .SystemType
                    .GetConfiguration()
                    .RawConfiguration
                    .MaxAntialiasingFor(settings));
    }
}
