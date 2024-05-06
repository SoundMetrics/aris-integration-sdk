// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SoundMetrics.Aris.Core.Raw
{
    using static BasicCalculations;
    using static AcousticSettingsAuto;

    internal delegate AcousticSettingsRaw AdjustRangeFn(
        AcousticSettingsRaw settings,
        ObservedConditions observedConditions,
        IAdjustWindowTerminus adjustmentStrategy,
        bool useMaxFrameRate,
        bool useAutoFrequency);

    internal static class PredefinedWindowSizes
    {
        private struct SystemTypeWindowSizing
        {
            public WindowBounds FixedWindowSizeShort;
            public WindowBounds FixedWindowSizeMedium;
            public WindowBounds FixedWindowSizeLong;
            public SystemConfiguration SystemConfiguration;
            public float StepwisePercent;
        }

        private static readonly ReadOnlyDictionary<SystemType, SystemTypeWindowSizing>
            windowSizingInfo = new ReadOnlyDictionary<SystemType, SystemTypeWindowSizing>(
                new Dictionary<SystemType, SystemTypeWindowSizing>
                {
                    {
                        SystemType.Aris1200,
                        new SystemTypeWindowSizing
                        {
                            FixedWindowSizeShort = new WindowBounds(3, 15),
                            FixedWindowSizeMedium = new WindowBounds(5, 30),
                            FixedWindowSizeLong = new WindowBounds(15, 60),
                            SystemConfiguration = SystemConfiguration.GetConfiguration(SystemType.Aris1200),
                            StepwisePercent = 10,
                        }
                    },
                    {
                        SystemType.Aris1800,
                        new SystemTypeWindowSizing
                        {
                            FixedWindowSizeShort = new WindowBounds(1, 7.5f),
                            FixedWindowSizeMedium = new WindowBounds(3, 15),
                            FixedWindowSizeLong = new WindowBounds(5, 30),
                            SystemConfiguration = SystemConfiguration.GetConfiguration(SystemType.Aris1800),
                            StepwisePercent = 10,
                        }
                    },
                    {
                        SystemType.Aris3000,
                        new SystemTypeWindowSizing
                        {
                            FixedWindowSizeShort = new WindowBounds(1, 5),
                            FixedWindowSizeMedium = new WindowBounds(2, 10),
                            FixedWindowSizeLong = new WindowBounds(4, 15),
                            SystemConfiguration = SystemConfiguration.GetConfiguration(SystemType.Aris3000),
                            StepwisePercent = 10,
                        }
                    },
                });
        internal static AcousticSettingsRaw ToShortWindow(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus adjustmentStrategy,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sizingInfo = windowSizingInfo[settings.SystemType];
            return ToFixedWindow(
                settings,
                observedConditions,
                adjustmentStrategy,
                sizingInfo.FixedWindowSizeShort,
                useMaxFrameRate,
                useAutoFrequency);
        }

        internal static AcousticSettingsRaw ToMediumWindow(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus adjustmentStrategy,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sizingInfo = windowSizingInfo[settings.SystemType];
            return ToFixedWindow(
                settings,
                observedConditions,
                adjustmentStrategy,
                sizingInfo.FixedWindowSizeMedium,
                useMaxFrameRate,
                useAutoFrequency);
        }

        internal static AcousticSettingsRaw ToLongWindow(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus adjustmentStrategy,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sizingInfo = windowSizingInfo[settings.SystemType];
            return ToFixedWindow(
                settings,
                observedConditions,
                adjustmentStrategy,
                sizingInfo.FixedWindowSizeLong,
                useMaxFrameRate,
                useAutoFrequency);
        }

        internal static AcousticSettingsRaw ToFixedWindow(
            AcousticSettingsRaw currentSettings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus adjustmentStrategy,
            in WindowBounds windowBounds,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
#if NOT_HERE
            var original = currentSettings;
            var sspd = observedConditions.SpeedOfSound(currentSettings.Salinity);

            var samplePeriod =
                CalculateSamplePeriod(windowBounds.WindowStart, windowBounds.WindowEnd, original.SampleCount, sspd)
                    .ConstrainTo(systemConfiguration.RawConfiguration.SamplePeriodLimits);
            var sampleStartDelay = 2 * windowBounds.WindowStart / sspd;

            return
                BuildNewWindowSettings(
                    original,
                    guidedSettingsMode,
                    observedConditions,
                    sampleStartDelay,
                    samplePeriod,
                    original.AntiAliasing,
                    original.InterpacketDelay,
                    automateFocusDistance: true,
                    useMaxFrameRate,
                    useAutoFrequency);
#else

            // ### REVIEW Naively replacing the above code with a call into
            // ### REVIEW this new code.
            return AcousticSettingsRawRangeOperations
                    .GetSettingsForSpecificRange(
                        currentSettings,
                        windowBounds,
                        observedConditions,
                        adjustmentStrategy,
                        useMaxFrameRate,
                        useAutoFrequency);

#endif
        }

        /// <summary>
        /// Builds acoustic settings using the new sample start delay and sample period.
        /// Sample count is meant to always be constant.
        /// </summary>
        /// <param name="original">The original settings.</param>
        /// <param name="sampleStartDelay">The new sample start delay.</param>
        /// <param name="samplePeriod">The new sample period.</param>
        /// <returns>A ready-to-use AcousticSettingsRaw.</returns>
        internal static
            AcousticSettingsRaw BuildNewWindowSettings(
                AcousticSettingsRaw original,
                ObservedConditions observedConditions,
                FineDuration sampleStartDelay,
                FineDuration samplePeriod,
                FineDuration antiAliasing,
                InterpacketDelaySettings interpacketDelay,
                bool automateFocusDistance,
                bool useMaxFrameRate,
                bool useAutoFrequency)
        {
            var sysCfg = SystemConfiguration.GetConfiguration(original.SystemType);
            var frameRate =
                Rate.Min(
                    original.FrameRate,
                    MaxFrameRate.CalculateMaximumFrameRateWithIntermediates(
                        sysCfg,
                        original.PingMode,
                        original.SampleCount,
                        sampleStartDelay,
                        samplePeriod,
                        antiAliasing,
                        interpacketDelay,
                        out var _,
                        out var _));

            var frequency = original.Frequency;
            var receiverGain = original.ReceiverGain;

            var windowStart =
                CalculateWindowStart(sampleStartDelay, original.Salinity, observedConditions);
            var windowLength =
                CalculateWindowLength(
                    original.SampleCount, samplePeriod, original.Salinity, observedConditions);
            var windowEnd = windowStart + windowLength;

            var pulseWidth =
                CalculateAutoPulseWidth(
                    original.SystemType,
                    observedConditions.WaterTemp,
                    original.Salinity,
                    windowEnd);

            var focusDistance =
                automateFocusDistance
                    ? (FocusDistance)(windowStart + (windowLength / 2))
                    : original.FocusDistance;

            var newSettings =
                new AcousticSettingsRaw(
                    systemType: original.SystemType,
                    frameRate: frameRate,
                    sampleCount: original.SampleCount,
                    sampleStartDelay: sampleStartDelay,
                    samplePeriod: samplePeriod,
                    pulseWidth: pulseWidth,
                    pingMode: original.PingMode,
                    enableTransmit: original.EnableTransmit,
                    frequency: frequency,
                    enable150Volts: original.Enable150Volts,
                    receiverGain: receiverGain,
                    focusDistance: focusDistance,
                    antiAliasing: antiAliasing,
                    interpacketDelay: interpacketDelay,
                    salinity: original.Salinity);

            newSettings = newSettings.WithMaxFrameRate(useMaxFrameRate);
            newSettings = useAutoFrequency
                            ? newSettings.WithAutomaticFrequency(observedConditions)
                            : newSettings;

            if (original.SampleCount != newSettings.SampleCount)
            {
                throw new ApplicationException(
                    $"sample count changed from [{original.SampleCount}] to [{newSettings.SampleCount}]");
            }

            return newSettings;
        }

        private static readonly FineDuration WindowTerminusAdjustment = (FineDuration)2;

        internal static AcousticSettingsRaw MoveWindowStartCloser(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus _,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => MoveWindowStartCloser(
                settings,
                observedConditions,
                useMaxFrameRate,
                useAutoFrequency);

        internal static AcousticSettingsRaw MoveWindowStartCloser(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation extends the window range. Because we're maintaining a fixed number of samples,
            // we must do this by increasing the sample period. The edge case is when we try to enlarge the window
            // to start before minimum sample start delay.

            if (settings.SamplePeriod >= cfg.RawConfiguration.SamplePeriodLimits.Maximum)
            {
                return settings;
            }

            if (settings.SampleStartDelay <= cfg.RawConfiguration.SampleStartDelayLimits.Minimum)
            {
                return settings;
            }

            var newSamplePeriod = settings.SamplePeriod + WindowTerminusAdjustment;
            var additionalSampleTime = WindowTerminusAdjustment * settings.SampleCount;
            var newSampleStartDelay =
                (settings.SampleStartDelay - additionalSampleTime)
                    .ConstrainTo(cfg.RawConfiguration.SampleStartDelayLimits);

            return BuildNewWindowSettings(
                settings,
                observedConditions,
                newSampleStartDelay,
                newSamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusDistance: true,
                useMaxFrameRate,
                useAutoFrequency);
        }

        private static FineDuration GetSamplePeriodToCover(Distance windowLength, int sampleCount, Velocity speedOfSound)
        {
            var duration = windowLength / speedOfSound;
            return 2 * (duration / sampleCount);
        }

        internal static AcousticSettingsRaw MoveWindowStartFarther(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus _,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => MoveWindowStartFarther(
                settings,
                observedConditions,
                useMaxFrameRate,
                useAutoFrequency);

        internal static AcousticSettingsRaw MoveWindowStartFarther(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation shortens the window range. Because we're maintaining a fixed number of samples,
            // we must do this by decreasing the sample period. The edge case is when we try to decrease the window
            // past its minimum sample period.

            if (settings.SamplePeriod <= cfg.RawConfiguration.SamplePeriodLimits.Minimum)
            {
                return settings;
            }

            if (settings.SampleStartDelay >= cfg.RawConfiguration.SampleStartDelayLimits.Maximum)
            {
                return settings;
            }

            var newSamplePeriod = settings.SamplePeriod - WindowTerminusAdjustment;
            var sampleTimeReduction = WindowTerminusAdjustment * settings.SampleCount;
            var newSampleStartDelay =
                (settings.SampleStartDelay + sampleTimeReduction)
                    .ConstrainTo(cfg.RawConfiguration.SampleStartDelayLimits);

            return BuildNewWindowSettings(
                settings,
                observedConditions,
                newSampleStartDelay,
                newSamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusDistance: true,
                useMaxFrameRate,
                useAutoFrequency);
        }

        internal static AcousticSettingsRaw MoveWindowEndCloser(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus _,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => MoveWindowEndCloser(
                settings,
                observedConditions,
                useMaxFrameRate,
                useAutoFrequency);

        internal static AcousticSettingsRaw MoveWindowEndCloser(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation shortens the window range. Because we're maintaining a fixed number of samples,
            // we must do this by decreasing the sample period. The edge case is when we try to decrease the window
            // past its minimum sample period.

            if (settings.SamplePeriod <= cfg.RawConfiguration.SamplePeriodLimits.Minimum)
            {
                return settings;
            }

            var newSamplePeriod = settings.SamplePeriod - WindowTerminusAdjustment;

            return BuildNewWindowSettings(
                settings,
                observedConditions,
                settings.SampleStartDelay,
                newSamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusDistance: true,
                useMaxFrameRate,
                useAutoFrequency);
        }

        internal static AcousticSettingsRaw MoveWindowEndFarther(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus _,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => MoveWindowEndFarther(
                settings,
                observedConditions,
                useMaxFrameRate,
                useAutoFrequency);


        /// <summary>
        /// Moves the range end outward in a chunk-wise fashion.
        /// For use with streamdeck-style operations.
        /// </summary>
        internal static AcousticSettingsRaw MoveWindowEndFarther(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation extends the window range. Because we're maintaining a fixed number of samples,
            // we must do this by increasing the sample period. The edge case is when we try to enlarge the window
            // to start before minimum sample start delay.

            if (settings.SamplePeriod >= cfg.RawConfiguration.SamplePeriodLimits.Maximum)
            {
                return settings;
            }

            var newSamplePeriod = settings.SamplePeriod + WindowTerminusAdjustment;

            return BuildNewWindowSettings(
                settings,
                observedConditions,
                settings.SampleStartDelay,
                newSamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusDistance: true,
                useMaxFrameRate,
                useAutoFrequency);
        }

        internal static AcousticSettingsRaw SlideWindowCloser(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus _,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => SlideWindowCloser(
                settings,
                observedConditions,
                useMaxFrameRate,
                useAutoFrequency);

        internal static AcousticSettingsRaw SlideWindowCloser(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation slides the window range. Because we're maintaining a fixed number of samples and
            // a fixed sample period, we must do this by decreasing the sample start delay. The edge case is when
            // we try to reduce the sample start delay below the minimum.

            if (settings.SampleStartDelay <= cfg.RawConfiguration.SampleStartDelayLimits.Minimum)
            {
                return settings;
            }

            var decrement = (settings.SampleCount * settings.SamplePeriod) / 3;
            var newSampleStartDelay = (settings.SampleStartDelay - decrement)
                .ConstrainTo(cfg.RawConfiguration.SampleStartDelayLimits);

            return BuildNewWindowSettings(
                settings,
                observedConditions,
                newSampleStartDelay,
                settings.SamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusDistance: true,
                useMaxFrameRate,
                useAutoFrequency);
        }

        internal static AcousticSettingsRaw SlideWindowFarther(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus _,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => SlideWindowFarther(
                settings,
                observedConditions,
                useMaxFrameRate,
                useAutoFrequency);

        internal static AcousticSettingsRaw SlideWindowFarther(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation slides the window range. Because we're maintaining a fixed number of samples and
            // a fixed sample period, we must do this by increasing the sample start delay. The edge case is when
            // we try to increase the sample start delay above the maximum.

            if (settings.SampleStartDelay >= cfg.RawConfiguration.SampleStartDelayLimits.Maximum)
            {
                return settings;
            }

            var increment = (settings.SampleCount * settings.SamplePeriod) / 3;
            var newSampleStartDelay = (settings.SampleStartDelay + increment)
                .ConstrainTo(cfg.RawConfiguration.SampleStartDelayLimits);

            return BuildNewWindowSettings(
                settings,
                observedConditions,
                newSampleStartDelay,
                settings.SamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusDistance: true,
                useMaxFrameRate,
                useAutoFrequency);
        }

    }
}
