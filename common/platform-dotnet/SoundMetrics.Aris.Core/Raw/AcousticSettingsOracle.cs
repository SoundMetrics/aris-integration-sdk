// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsConstraints;
    using static Math;

    [Flags]
    public enum AutomaticAcousticSettings
    {
        None = 0,

        FocusPosition = 0b0000_0001,
        Frequency = 0b0000_0010,
        PulseWidth = 0b0000_0100,

        All = 0b1111_1111
    }

    public static class AcousticSettingsOracle
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
            Distance focusPosition,
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
                focusPosition,
                antiAliasing,
                interpacketDelay,
                salinity);

            var allowed = ApplyAllConstraints(requested);
            return allowed;
        }

        internal static AcousticSettingsRaw ApplyAllConstraints(AcousticSettingsRaw settings)
        {
            var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            settings = UpdateFrameRate(
                settings,
                ConstrainFrameRate(
                    settings.FrameRate,
                    sysCfg,
                    settings.PingMode,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.AntiAliasing,
                    settings.InterpacketDelay));

            settings = settings.WithLimitedPulseWidth(settings.PulseWidth);

            // Last: antialiasing
            settings =
                UpdateAntiAliasing(
                    settings,
                    settings.ConstrainAntiAliasing());

            return settings;
        }

        internal static FineDuration LimitPulseWidth(
            SystemType systemType,
            FineDuration requestedPulseWidth,
            Frequency frequency,
            Rate frameRate)
        {
            var rawCfg = systemType.GetConfiguration().RawConfiguration;
            var constrainedValue =
                requestedPulseWidth.ConstrainTo(rawCfg.PulseWidthRange);

            return
                rawCfg
                    .LimitPulseWidthEnergy(frequency, constrainedValue, frameRate)
                    .Floor;

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

            return new AcousticSettingsRaw(
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

            return UpdateFrameRate(settings, allowedFrameRate);
        }

        public static AcousticSettingsRaw WithMaxFrameRate(
            this AcousticSettingsRaw settings,
            bool enable)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return enable
                ? settings
                    .WithFrameRate(settings.MaximumFrameRate)
                    .ApplyAllConstraints()
                : settings;
        }

        private static AcousticSettingsRaw UpdateFrameRate(
            AcousticSettingsRaw settings, Rate newFrameRate)
            => settings.FrameRate == newFrameRate
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    newFrameRate,
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
                    settings.Salinity);

        public static AcousticSettingsRaw WithTransmit(
            this AcousticSettingsRaw settings,
            bool enableTransmit)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return settings.EnableTransmit == enableTransmit
                ? settings
                : new AcousticSettingsRaw(
                        settings.SystemType,
                        settings.FrameRate,
                        settings.SampleCount,
                        settings.SampleStartDelay,
                        settings.SamplePeriod,
                        settings.PulseWidth,
                        settings.PingMode,
                        enableTransmit: enableTransmit,
                        settings.Frequency,
                        settings.Enable150Volts,
                        settings.ReceiverGain,
                        settings.FocusDistance,
                        settings.AntiAliasing,
                        settings.InterpacketDelay,
                        settings.Salinity);
        }

        private static AcousticSettingsRaw UpdateAntiAliasing(
            AcousticSettingsRaw settings,
            FineDuration antiAliasing)
        => settings.AntiAliasing == antiAliasing
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
                    antiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

        public static AcousticSettingsRaw WithFocusPosition(
            this AcousticSettingsRaw settings,
            Distance newFocusPosition)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return
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
                    newFocusPosition,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity)
                .ApplyAllConstraints();
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

            return
                UpdateAntiAliasing(settings, newDelay)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }

        internal static AcousticSettingsRaw WithSamplePeriod(
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
            var constrainedSamplePeriod =
                samplePeriod.ConstrainTo(sysCfg.RawConfiguration.SamplePeriodRange);

            if (constrainedSamplePeriod == settings.SamplePeriod && !useMaxFrameRate)
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

            return
                newSettings
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
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
            var constrainedSampleStartDelay =
                sampleStartDelay.ConstrainTo(sysCfg.RawConfiguration.SampleStartDelayRange);

            if (constrainedSampleStartDelay == settings.SampleStartDelay && !useMaxFrameRate)
            {
                return settings;
            }

            var newSettings =
                new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    constrainedSampleStartDelay,
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

            return
                newSettings
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }

        public static AcousticSettingsRaw WithAutomaticFrequency(
            this AcousticSettingsRaw settings,
            ObservedConditions observedConditions)
            => settings.WithAutomaticSettings(
                observedConditions,
                AutomaticAcousticSettings.Frequency);

        internal static AcousticSettingsRaw WithAutomaticSettings(
            this AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            AutomaticAcousticSettings automaticFlags)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            if ((automaticFlags & AutomaticAcousticSettings.FocusPosition) != 0)
            {
                var windowMidPoint = settings.WindowMidPoint(observedConditions);
                settings = settings.WithFocusPosition(windowMidPoint);
            }

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (settings is null) throw new Exception("Settings became null");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            if ((automaticFlags & AutomaticAcousticSettings.Frequency) != 0)
            {
                var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);
                var windowEnd = settings.WindowEnd(observedConditions);
                var isLongerRange = windowEnd > sysCfg.FrequencyCrossover;
                var frequency = isLongerRange ? Frequency.Low : Frequency.High;

                settings = settings.WithFrequency(frequency);
            }

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (settings is null) throw new Exception("Settings became null");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            // Pulse width depends on frequency, so it's addressed *after* frequency.
            if ((automaticFlags & AutomaticAcousticSettings.PulseWidth) != 0)
            {
                var automaticPulseWidth = CalculateAutomaticPulseWidth(settings, observedConditions);
                settings = settings.WithPulseWidth(automaticPulseWidth);
            }

            return settings.ApplyAllConstraints();
        }

        public static AcousticSettingsRaw WithFrequency(
            this AcousticSettingsRaw settings,
            Frequency frequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return frequency == settings.Frequency
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
            return
                newSettings
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }

        public static AcousticSettingsRaw WithSalinity(
            this AcousticSettingsRaw settings,
            Salinity salinity)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return salinity == settings.Salinity
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
        }

        public static AcousticSettingsRaw WithReceiverGain(
            this AcousticSettingsRaw settings,
            float gain)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return gain == settings.ReceiverGain
                ? settings
                : throw new NotImplementedException();
        }

        private static FineDuration CalculateAutomaticPulseWidth(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions)
        {
            // Based on ARIScope 2's auto pulse width
            var sysCfg = settings.SystemType.GetConfiguration();
            var rawCfg = sysCfg.RawConfiguration;

            var multiplier = rawCfg.GetPulseWidthMultiplierFor(settings.Frequency);
            var windowEnd = settings.WindowEnd(observedConditions);

            var pulseWidth =
                ((FineDuration)(uint)((multiplier * windowEnd.Meters) + 0.5))
                    .ConstrainTo(rawCfg.AllowedPulseWidthRangeFor(settings.Frequency));
            return pulseWidth;
        }

        public static AcousticSettingsRaw WithPulseWidth(
            this AcousticSettingsRaw settings,
            FineDuration requestedPulseWidth)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return requestedPulseWidth == settings.PulseWidth
                ? settings
                : settings.WithLimitedPulseWidth(requestedPulseWidth);
        }

        public static AcousticSettingsRaw WithTransmitEnable(
            this AcousticSettingsRaw settings,
            bool enable)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return enable == settings.EnableTransmit
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    enable,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);
        }

        public static AcousticSettingsRaw With150VoltsEnable(
            this AcousticSettingsRaw settings,
            bool enable)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return enable == settings.Enable150Volts
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
                    enable,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);
        }

        public static AcousticSettingsRaw WithSampleCount(
            this AcousticSettingsRaw settings,
            int sampleCount)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            return sampleCount == settings.SampleCount
                ? settings
                : throw new NotImplementedException();
        }

        private static AcousticSettingsRaw WithLimitedPulseWidth(
            this AcousticSettingsRaw settings,
            FineDuration requestedPulseWidth)
        {
            var limitedPulseWidth =
                LimitPulseWidth(
                    settings.SystemType,
                    requestedPulseWidth,
                    settings.Frequency,
                    settings.FrameRate);

            return limitedPulseWidth == settings.PulseWidth
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
        }
    }
}
