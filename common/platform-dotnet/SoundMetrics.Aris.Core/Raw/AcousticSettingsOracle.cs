// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsConstraints;

    [Flags]
    public enum AutomaticAcousticSettings
    {
        FocusPosition = 0b0001,
        Frequency = 0b0010,

        FrequencyAndFocus = Frequency | FocusPosition,
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

            // ### TODO much else
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

            // Limit pulse width
            {
                var limitedPulseWidth =
                    LimitPulseWidth(
                        settings.SystemType,
                        settings.PulseWidth,
                        settings.Frequency,
                        settings.FrameRate);
                settings =
                    limitedPulseWidth == settings.PulseWidth
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

        public static AcousticSettingsRaw WithFrameRate(
            this AcousticSettingsRaw settings,
            Rate requestedFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

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

            if (!enable)
            {
                return settings;
            }

            return settings
                    .WithFrameRate(settings.MaximumFrameRate)
                    .ApplyAllConstraints();
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

        public static AcousticSettingsRaw WithAutomaticSettings(
            this AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            AutomaticAcousticSettings automaticFlags)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            if ((automaticFlags & AutomaticAcousticSettings.FocusPosition) != 0)
            {
                automaticFlags ^= AutomaticAcousticSettings.FocusPosition;

                var windowMidPoint = settings.WindowMidPoint(observedConditions);
                settings = settings.WithFocusPosition(windowMidPoint);
            }

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (settings is null) throw new Exception("Settings became null");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            if ((automaticFlags & AutomaticAcousticSettings.Frequency) != 0)
            {
                automaticFlags ^= AutomaticAcousticSettings.Frequency;

                var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);
                var isLongerRange = settings.WindowEnd(observedConditions) > sysCfg.FrequencyCrossover;
                var frequency = isLongerRange ? Frequency.Low : Frequency.High;

                settings = settings.WithFrequency(frequency);
            }

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (settings is null) throw new Exception("Settings became null");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            if (automaticFlags != 0)
            {
                throw new NotImplementedException($"automatic setting(s) not implemented: {automaticFlags}");
            }

            return settings.ApplyAllConstraints();
        }

        public static AcousticSettingsRaw WithFrequency(
            this AcousticSettingsRaw settings,
            Frequency frequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            if (frequency == settings.Frequency) return settings;

            return new AcousticSettingsRaw(
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

            return new AcousticSettingsRaw(
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

            if (gain == settings.ReceiverGain) return settings;

            throw new NotImplementedException();
        }

        //public static AcousticSettingsRaw WithTransmitEnable(
        //    this AcousticSettingsRaw settings,
        //    bool enable)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRaw With150VoltsEnable(
        //    this AcousticSettingsRaw settings,
        //    bool enable)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRaw WithSampleCount(
        //    this AcousticSettingsRaw settings,
        //    int sampleCount)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRaw WithPulseWidth(
        //    this AcousticSettingsRaw settings,
        //    FineDuration pulseWidth)
        //{
        //    throw new NotImplementedException();

        //    /*
        //            var systemType = SoundMetrics.Aris.Core.SystemType.GetFromIntegralValue((int)connection.SystemType);
        //            var pulseWidth = FineDuration.FromMicroseconds(pulseWidthUsec);
        //            var frequency = (SoundMetrics.Aris.Core.Frequency)settings.frequency;
        //            var frameRate = (Rate)settings.frameRate;

        //            safePulseWidth =
        //                (uint)LimitPulseWidth(
        //                        systemType,
        //                        pulseWidth,
        //                        frequency,
        //                        frameRate)
        //                        .Floor
        //                        .TotalMicroseconds;
        //            settings.pulseWidth = safePulseWidth;
        //     */
        //}
    }
}
