// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Diagnostics;
using System.Threading;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsConstraints;
    using static AcousticSettingsRawExtensions;
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

            var settings2 = UpdateFrameRate(
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

            var settings3 = settings2.WithLimitedPulseWidth(settings2.PulseWidth);

            // Last: antialiasing
            var result =
                UpdateAntiAliasing(
                    settings3,
                    settings3.ConstrainAntiAliasing());

            LogSettingsChangeResult($"{nameof(ApplyAllConstraints)}", settings, result);

            return result;
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

            LogSettingsChangeResult($"{nameof(WithPingMode)}", settings, result);

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

            var result = UpdateFrameRate(settings, allowedFrameRate);

            LogSettingsChangeResult($"{nameof(WithFrameRate)}", settings, result);

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

            LogSettingsChangeResult($"{nameof(WithMaxFrameRate)}", settings, result);

            return result;
        }

        private static AcousticSettingsRaw UpdateFrameRate(
            AcousticSettingsRaw settings, Rate newFrameRate)
        {
            var result = settings.FrameRate == newFrameRate
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

            LogSettingsChangeResult($"{nameof(UpdateFrameRate)}", settings, result);

            return result;
        }

        public static AcousticSettingsRaw WithTransmit(
            this AcousticSettingsRaw settings,
            bool enableTransmit)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = settings.EnableTransmit == enableTransmit
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

            LogSettingsChangeResult($"{nameof(WithTransmit)}", settings, result);

            return result;
        }

        private static AcousticSettingsRaw UpdateAntiAliasing(
            AcousticSettingsRaw settings,
            FineDuration antiAliasing)

        {
            var result = settings.AntiAliasing == antiAliasing
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

            LogSettingsChangeResult($"{nameof(UpdateAntiAliasing)}", settings, result);

            return result;
        }

        public static AcousticSettingsRaw WithFocusPosition(
            this AcousticSettingsRaw settings,
            Distance newFocusPosition)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result =
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

            LogSettingsChangeResult($"{nameof(WithFocusPosition)}", settings, result);

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

            LogSettingsChangeResult($"{nameof(WithAntiAliasing)}", settings, result);

            return result;
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

            var result =
                newSettings
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            LogSettingsChangeResult($"{nameof(WithSamplePeriod)}", settings, result);

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

            var result =
                newSettings
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            LogSettingsChangeResult($"{nameof(WithSampleStartDelay)}", settings, result);

            return result;
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

            var result = settings.ApplyAllConstraints();

            LogSettingsChangeResult($"{nameof(WithAutomaticSettings)}", settings, result);

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

            LogSettingsChangeResult($"{nameof(WithFrequency)}", settings, result);

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

            LogSettingsChangeResult($"{nameof(WithInterpacketDelay)}", settings, result);

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

            LogSettingsChangeResult($"{nameof(WithSalinity)}", settings, result);

            return result;
        }

        public static AcousticSettingsRaw WithReceiverGain(
            this AcousticSettingsRaw settings,
            int gain)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sysCfg = settings.SystemType.GetConfiguration();
            var constrainedValue = gain.ConstrainTo(sysCfg.ReceiverGainRange);

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

            LogSettingsChangeResult($"{nameof(WithReceiverGain)}", settings, result);

            return result;
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

            var result = requestedPulseWidth == settings.PulseWidth
                ? settings
                : settings.WithLimitedPulseWidth(requestedPulseWidth);

            LogSettingsChangeResult($"{nameof(WithPulseWidth)}", settings, result);

            return result;
        }

        public static AcousticSettingsRaw WithTransmitEnable(
            this AcousticSettingsRaw settings,
            bool enable)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = enable == settings.EnableTransmit
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

            LogSettingsChangeResult($"{nameof(WithTransmitEnable)}", settings, result);

            return result;
        }

        public static AcousticSettingsRaw With150VoltsEnable(
            this AcousticSettingsRaw settings,
            bool enable)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = enable == settings.Enable150Volts
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

            LogSettingsChangeResult($"{nameof(With150VoltsEnable)}", settings, result);

            return result;
        }

        public static AcousticSettingsRaw WithSampleCount(
            this AcousticSettingsRaw settings,
            int sampleCount)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = sampleCount == settings.SampleCount
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    sampleCount,
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

            LogSettingsChangeResult($"{nameof(WithSampleCount)}", settings, result);

            return result;
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

            LogSettingsChangeResult($"{nameof(WithLimitedPulseWidth)}", settings, result);

            return result;
        }

        private const string LogSettingsTag = "#aris.settings";

        private static string GetLogSettingsPrefix()
            => $"{LogSettingsTag} {{tid.#={Thread.CurrentThread.ManagedThreadId}.{SettingsChangeLoggingCounter}}}";

        internal static void LogSettingsChangeContext(string context)
        {
            if (IsSettingsChangeLoggingEnabled)
            {
                Trace.TraceInformation($"{GetLogSettingsPrefix()} change context: {context}");
            }
        }

        private static void LogSettingsChangeResult(
            string contextName,
            AcousticSettingsRaw a,
            AcousticSettingsRaw b)
        {
            if (IsSettingsChangeLoggingEnabled)
            {
                if (GetDifferences(a, b, out var differences))
                {
                    Trace.TraceInformation($"{GetLogSettingsPrefix()} [{contextName}]: {differences}");
                }
            }
        }
    }
}
