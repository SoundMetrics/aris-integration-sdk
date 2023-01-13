// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsConstraints;
    using static AcousticSettingsRawImmutable;
    using Immutable = AcousticSettingsRawImmutable;

    [Flags]
    public enum AutomaticAcousticSettings
    {
        None = 0,

        FocusDistance = 0b0000_0001,
        Frequency = 0b0000_0010,
        PulseWidth = 0b0000_0100,

        All = 0b1111_1111
    }

    public static class AcousticSettingsRawSafeUpdates
    {
        /// <summary>
        /// Initializes a new instance of acoustic settings.
        /// </summary>
        public static AcousticSettingsRaw Initialize(
            SystemType systemType,
            Rate frameRate,
            int sampleCount,
            FineDuration sampleStartDelay,
            FineDuration samplePeriod,
            FineDuration pulseWidth,
            PingMode pingMode,
            bool enableTransmit,
            Frequency frequency,
            bool enable150Volts,
            int receiverGain,
            Distance focusDistance,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay,
            Salinity salinity)
        {
            // This method is the only public route to a new instance of AcousticSettingsRaw.
            // This promotes strong control over the values in AcousticSettingsRaw.

            var requested = new AcousticSettingsRaw(
                systemType,
                frameRate,
                sampleCount,
                sampleStartDelay,
                samplePeriod,
                pulseWidth,
                pingMode,
                enableTransmit,
                frequency,
                enable150Volts,
                receiverGain,
                focusDistance,
                antiAliasing,
                interpacketDelay,
                salinity);

            var allowed = ApplyAllConstraints(requested);
            return allowed;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public static AcousticSettingsRaw ApplyAllConstraints(
            this AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            var settings2 = settings.UpdateFrameRate(
                ConstrainFrameRate(
                    settings.FrameRate,
                    sysCfg,
                    settings.PingMode,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.AntiAliasing,
                    settings.InterpacketDelay));

            if (settings2 is null)
            {
                throw new NullReferenceException($"return from {nameof(Immutable.UpdateFrameRate)}");
            }

            var settings3 =
                settings2.WithLimitedPulseWidth(settings2.PulseWidth);

            if (settings3 is null)
            {
                throw new NullReferenceException($"return from {nameof(WithLimitedPulseWidth)}");
            }

            // Last: antialiasing
            var result =
                UpdateAntiAliasing(
                    settings3,
                    settings3.ConstrainAntiAliasing());

            return result;
        }

        internal static FineDuration LimitPulseWidth(
            SystemType systemType,
            FineDuration requestedPulseWidth,
            Frequency frequency,
            Rate frameRate)
        {
            var rawCfg = systemType.GetConfiguration().RawConfiguration;
            var validRange = rawCfg.GetPulseWidthLimitsFor(frequency).Limits;
            var constrainedValue = requestedPulseWidth.ConstrainTo(validRange);

            var result =
                rawCfg
                    .LimitPulseWidthEnergy(frequency, constrainedValue, frameRate)
                    .Floor;
            return result;
        }

        public static AcousticSettingsRaw WithPingMode(
            this AcousticSettingsRaw settings,
            PingMode pingMode)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            if (!settings.SystemType.GetConfiguration().IsValidPingMode(pingMode))
            {
                throw new ArgumentOutOfRangeException(
                    $"Invalid ping mode [{pingMode}] for system type [{settings.SystemType}]");
            }

            var result = new AcousticSettingsRaw(
                        settings.SystemType,
                        settings.FrameRate,
                        settings.SampleCount,
                        settings.SampleStartDelay,
                        settings.SamplePeriod,
                        settings.PulseWidth,
                        pingMode,
                        settings.EnableTransmit,
                        settings.Frequency,
                        settings.Enable150Volts,
                        settings.ReceiverGain,
                        settings.FocusDistance,
                        settings.AntiAliasing,
                        settings.InterpacketDelay,
                        settings.Salinity)
                    .ApplyAllConstraints();

            return result;
        }

        public static AcousticSettingsRaw WithFrameRate(
            this AcousticSettingsRaw settings,
            Rate requestedFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            if (requestedFrameRate == settings.FrameRate)
            {
                return settings;
            }

            var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);
            var allowedFrameRate =
                ConstrainFrameRate(
                    requestedFrameRate,
                    sysCfg,
                    settings.PingMode,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.AntiAliasing,
                    settings.InterpacketDelay);

            var result = settings.UpdateFrameRate(allowedFrameRate);

            return result;
        }

        public static AcousticSettingsRaw WithMaxFrameRate(
            this AcousticSettingsRaw settings,
            bool enable)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = enable
                ? settings
                    .WithFrameRate(settings.MaximumFrameRate)
                    .ApplyAllConstraints()
                : settings;

            return result;
        }

        public static AcousticSettingsRaw WithAntiAliasing(
            this AcousticSettingsRaw settings,
            FineDuration newDelay,
            bool useMaxFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (newDelay < FineDuration.Zero) throw new ArgumentOutOfRangeException(nameof(newDelay), "Argument value is negative");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            var result =
                UpdateAntiAliasing(settings, newDelay)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            return result;
        }

        public static AcousticSettingsRaw WithSamplePeriod(
            this AcousticSettingsRaw settings,
            FineDuration samplePeriod,
            bool useMaxFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (samplePeriod <= FineDuration.Zero)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(nameof(samplePeriod), "Negative or zero value");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var sysCfg = settings.SystemType.GetConfiguration();
            var validRange = sysCfg.RawConfiguration.SamplePeriodLimits;
            var constrainedSamplePeriod = samplePeriod.ConstrainTo(validRange);

            if ((constrainedSamplePeriod == settings.SamplePeriod) && !useMaxFrameRate)
            {
                return settings;
            }

            var newSettings =
                new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    constrainedSamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            var result =
                newSettings
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            return result;
        }

        public static AcousticSettingsRaw WithSampleStartDelay(
            this AcousticSettingsRaw settings,
            FineDuration sampleStartDelay,
            bool useMaxFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (sampleStartDelay <= FineDuration.Zero)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(nameof(sampleStartDelay), "Negative or zero value");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var sysCfg = settings.SystemType.GetConfiguration();
            var validRange = sysCfg.RawConfiguration.SampleStartDelayLimits;
            var constrained = sampleStartDelay.ConstrainTo(validRange);

            if (constrained == settings.SampleStartDelay && !useMaxFrameRate)
            {
                return settings;
            }

            var newSettings =
                new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    constrained,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            var result =
                newSettings
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            return result;
        }

        public static AcousticSettingsRaw WithAutomaticFrequency(
            this AcousticSettingsRaw settings,
            ObservedConditions observedConditions)
        {
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            return settings.WithAutomaticSettings(
                observedConditions,
                AutomaticAcousticSettings.Frequency);
        }

        public static AcousticSettingsRaw WithAutomaticFocusDistance(
            this AcousticSettingsRaw settings,
            ObservedConditions observedConditions)
        {
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            return settings.WithAutomaticSettings(
                observedConditions,
                AutomaticAcousticSettings.FocusDistance);
        }

        public static AcousticSettingsRaw WithAutomaticSettings(
            this AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            AutomaticAcousticSettings automaticFlags)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            var windowBounds = settings.WindowBounds(observedConditions);

            if ((automaticFlags & AutomaticAcousticSettings.FocusDistance) != 0)
            {
                var midpoint = windowBounds.Midpoint;
                Debug.WriteLine($"({nameof(WithAutomaticSettings)}): auto focus to midpoint [{midpoint}]; windowBounds=[{windowBounds}]");
                settings = settings.WithFocusDistance(midpoint);
            }

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (settings is null) throw new Exception("Settings became null");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            if ((automaticFlags & AutomaticAcousticSettings.Frequency) != 0)
            {
                var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);
                var windowEnd = windowBounds.WindowEnd;

                var frequency = sysCfg.CalculateBestFrequency(
                    observedConditions.WaterTemp,
                    settings.Salinity,
                    windowEnd);

                settings = settings.WithFrequency(frequency);
            }

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (settings is null) throw new Exception("Settings became null");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            // Pulse width depends on frequency, so it's addressed *after* frequency.
            if ((automaticFlags & AutomaticAcousticSettings.PulseWidth) != 0)
            {
                var windowEnd = windowBounds.WindowEnd;
                var automaticPulseWidth =
                    AcousticSettingsAuto.CalculateAutoPulseWidth(
                        settings.SystemType,
                        observedConditions.WaterTemp,
                        settings.Salinity,
                        windowEnd);
;
                settings = settings.WithPulseWidth(automaticPulseWidth);
            }

            var result = settings.ApplyAllConstraints();

            return result;
        }

        public static AcousticSettingsRaw WithFrequency(
            this AcousticSettingsRaw settings,
            Frequency frequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = frequency == settings.Frequency
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity)
                    .ApplyAllConstraints();

            return result;
        }

        public static AcousticSettingsRaw WithInterpacketDelay(
            this AcousticSettingsRaw settings,
            InterpacketDelaySettings newInterpacketDelay,
            bool useMaxFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            if (newInterpacketDelay == settings.InterpacketDelay) return settings;

            var newSettings =
                new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    newInterpacketDelay,
                    settings.Salinity);
            var result =
                newSettings
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            return result;
        }

        public static AcousticSettingsRaw WithSalinity(
            this AcousticSettingsRaw settings,
            Salinity salinity)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = salinity == settings.Salinity
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    salinity);

            return result;
        }

        public static AcousticSettingsRaw WithReceiverGain(
            this AcousticSettingsRaw settings,
            int gain)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sysCfg = settings.SystemType.GetConfiguration();
            var constrainedValue = gain.ConstrainTo(sysCfg.ReceiverGainLimits);

            var result = constrainedValue == settings.ReceiverGain
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    constrainedValue,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            return result;
        }

        public static AcousticSettingsRaw WithPulseWidth(
            this AcousticSettingsRaw settings,
            FineDuration requestedPulseWidth)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = requestedPulseWidth == settings.PulseWidth
                ? settings
                : settings.WithLimitedPulseWidth(requestedPulseWidth);

            return result;
        }

        private static AcousticSettingsRaw WithLimitedPulseWidth(
            this AcousticSettingsRaw settings,
            FineDuration requestedPulseWidth)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var limitedPulseWidth =
                LimitPulseWidth(
                    settings.SystemType,
                    requestedPulseWidth,
                    settings.Frequency,
                    settings.FrameRate);

            var result = limitedPulseWidth == settings.PulseWidth
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    limitedPulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            return result;
        }

        private const string LogSettingsTag = "#aris.settings";

        private static string GetLogSettingsPrefix() => $"{LogSettingsTag} ";
    }
}
