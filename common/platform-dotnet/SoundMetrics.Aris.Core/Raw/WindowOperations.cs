﻿// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsRawCalculations;

    public delegate AcousticSettingsRaw AdjustRangeFn(AcousticSettingsRaw currentSettings);

    public static class WindowOperations
    {
        private struct FixedWindowSize
        {
            public Distance WindowStart;
            public Distance WindowEnd;

            public FixedWindowSize(Distance windowStart, Distance windowEnd)
            {
                WindowStart = windowStart;
                WindowEnd = windowEnd;
            }

            public FixedWindowSize(float windowStart, float windowEnd)
            {
                WindowStart = Distance.FromMeters(windowStart);
                WindowEnd = Distance.FromMeters(windowEnd);
            }

            public override string ToString()
                => $"WindowStart=[{WindowStart}]; WindowEnd=[{WindowEnd}]";
        }

        private struct SystemTypeWindowSizing
        {
            public FixedWindowSize FixedWindowSizeShort;
            public FixedWindowSize FixedWindowSizeMedium;
            public FixedWindowSize FixedWindowSizeLong;
            public SystemConfiguration SystemConfiguration;
            public float StepwisePercent;
        }

        private static readonly Dictionary<SystemType, SystemTypeWindowSizing>
            windowSizingInfo = GetFixedWindowSizes();

        private static Dictionary<SystemType, SystemTypeWindowSizing> GetFixedWindowSizes()
        {
            return new Dictionary<SystemType, SystemTypeWindowSizing>
            {
                {
                    SystemType.Aris1200,
                    new SystemTypeWindowSizing
                    {
                        FixedWindowSizeShort = new FixedWindowSize(3, 15),
                        FixedWindowSizeMedium = new FixedWindowSize(5, 60),
                        FixedWindowSizeLong = new FixedWindowSize(15, 60),
                        SystemConfiguration = SystemConfiguration.GetConfiguration(SystemType.Aris1200),
                        StepwisePercent = 10,
                    }
                },
                {
                    SystemType.Aris1800,
                    new SystemTypeWindowSizing
                    {
                        FixedWindowSizeShort = new FixedWindowSize(1, 7.5f),
                        FixedWindowSizeMedium = new FixedWindowSize(3, 15),
                        FixedWindowSizeLong = new FixedWindowSize(5, 30),
                        SystemConfiguration = SystemConfiguration.GetConfiguration(SystemType.Aris1800),
                        StepwisePercent = 10,
                    }
                },
                {
                    SystemType.Aris3000,
                    new SystemTypeWindowSizing
                    {
                        FixedWindowSizeShort = new FixedWindowSize(1, 5),
                        FixedWindowSizeMedium = new FixedWindowSize(2, 10),
                        FixedWindowSizeLong = new FixedWindowSize(4, 15),
                        SystemConfiguration = SystemConfiguration.GetConfiguration(SystemType.Aris3000),
                        StepwisePercent = 10,
                    }
                },
            };
        }

        public static AcousticSettingsRaw ToShortWindow(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sizingInfo = windowSizingInfo[settings.SystemType];
            return ToFixedWindow(
                settings,
                sizingInfo.SystemConfiguration,
                sizingInfo.FixedWindowSizeShort);
        }

        public static AcousticSettingsRaw ToMediumWindow(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sizingInfo = windowSizingInfo[settings.SystemType];
            return ToFixedWindow(
                settings,
                sizingInfo.SystemConfiguration,
                sizingInfo.FixedWindowSizeMedium);
        }

        public static AcousticSettingsRaw ToLongWindow(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sizingInfo = windowSizingInfo[settings.SystemType];
            return ToFixedWindow(
                settings,
                sizingInfo.SystemConfiguration,
                sizingInfo.FixedWindowSizeLong);
        }

        private static AcousticSettingsRaw ToFixedWindow(
            AcousticSettingsRaw currentSettings,
            SystemConfiguration systemConfiguration,
            in FixedWindowSize windowSize)
        {
            var sspd = currentSettings.SonarEnvironment.SpeedOfSound;

            var original = currentSettings;

            var sampleStartDelay =
                currentSettings.CalculateSampleStartDelay()
                    .ConstrainTo(systemConfiguration.RawConfiguration.SampleStartDelayRange);

            var actualWindowStart = TimeToDistance(sampleStartDelay, sspd) / 2;

            var samplePeriod =
                CalculateSamplePeriod(actualWindowStart, windowSize.WindowEnd, original.SampleCount, sspd)
                    .ConstrainTo(systemConfiguration.RawConfiguration.SamplePeriodRange);

            return
                BuildNewWindowSettings(
                    original,
                    sampleStartDelay,
                    samplePeriod,
                    original.AntiAliasing,
                    original.InterpacketDelay,
                    automateFocusPosition: true);
        }

        public static Distance TimeToDistance(FineDuration duration, Velocity sspd)
            => sspd * duration;

        private static FineDuration CalculateSamplePeriod(
            Distance windowStart,
            Distance windowEnd,
            int sampleCount,
            Velocity sspd)
            =>
                // (2 * WL) / (N * SSPD)
                ((2 * (windowEnd - windowStart)) / (sampleCount * sspd))
                    .Ceiling;

        /// <summary>
        /// Builds acoustic settings using the new sample start delay and sample period.
        /// Sample count is meant to always be constant.
        /// </summary>
        /// <param name="original">The original settings.</param>
        /// <param name="sampleStartDelay">The new sample start delay.</param>
        /// <param name="samplePeriod">The new sample period.</param>
        /// <returns>A ready-to-use AcousticSettingsRaw.</returns>
        private static
            AcousticSettingsRaw BuildNewWindowSettings(
                AcousticSettingsRaw original,
                FineDuration sampleStartDelay,
                FineDuration samplePeriod,
                FineDuration antiAliasing,
                InterpacketDelaySettings interpacketDelay,
                bool automateFocusPosition)
        {
            var sysCfg = SystemConfiguration.GetConfiguration(original.SystemType);
            var frameRate =
                Rate.Min(
                    original.FrameRate,
                    MaxFrameRate.DetermineMaximumFrameRate(
                        sysCfg,
                        original.PingMode,
                        original.SampleCount,
                        sampleStartDelay,
                        samplePeriod,
                        antiAliasing,
                        interpacketDelay));

            var pingsPerFrame = original.PingMode.PingsPerFrame;
            var framePeriod = 1 / frameRate;
            var cyclePeriod = framePeriod / pingsPerFrame;

            // ### Leaving these as-is for now.
            var pulseWidth = original.PulseWidth;
            var frequency = original.Frequency;
            var receiverGain = original.ReceiverGain;

            var windowStart = CalculateWindowStart(sampleStartDelay, original.SonarEnvironment);
            var windowLength = CalculateWindowLength(original.SampleCount, samplePeriod, original.SonarEnvironment);

            var focusPosition =
                automateFocusPosition
                    ? windowStart + (windowLength / 2)
                    : original.FocusPosition;

            var newSettings =
                new AcousticSettingsRaw(
                    systemType: original.SystemType,
                    frameRate: frameRate,
                    sampleCount: original.SampleCount,
                    sampleStartDelay: sampleStartDelay,
                    cyclePeriod: cyclePeriod,
                    samplePeriod: samplePeriod,
                    pulseWidth: pulseWidth,
                    pingMode: original.PingMode,
                    enableTransmit: original.EnableTransmit,
                    frequency: frequency,
                    enable150Volts: original.Enable150Volts,
                    receiverGain: receiverGain,
                    focusPosition: focusPosition,
                    antiAliasing: antiAliasing,
                    interpacketDelay: interpacketDelay,
                    sonarEnvironment: original.SonarEnvironment);

            if (original.SampleCount != newSettings.SampleCount)
            {
                throw new ApplicationException(
                    $"sample count changed from [{original.SampleCount}] to [{newSettings.SampleCount}]");
            }

            return newSettings;
        }

        private static readonly FineDuration WindowTerminusAdjustment = FineDuration.FromMicroseconds(2);

        public static AcousticSettingsRaw MoveWindowStartIn(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation extends the window range. Because we're maintaining a fixed number of samples,
            // we must do this by increasing the sample period. The edge case is when we try to enlarge the window
            // to start before minimum sample start delay.

            if (settings.SamplePeriod >= cfg.RawConfiguration.SamplePeriodRange.Maximum)
            {
                return settings;
            }

            if (settings.SampleStartDelay <= cfg.RawConfiguration.SampleStartDelayRange.Minimum)
            {
                return settings;
            }

            var newSamplePeriod = settings.SamplePeriod + WindowTerminusAdjustment;
            var additionalSampleTime = WindowTerminusAdjustment * settings.SampleCount;
            var newSampleStartDelay =
                (settings.SampleStartDelay - additionalSampleTime)
                    .ConstrainTo(cfg.RawConfiguration.SampleStartDelayRange);

            return BuildNewWindowSettings(
                settings,
                newSampleStartDelay,
                newSamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusPosition: true);
        }

        private static FineDuration GetSamplePeriodToCover(Distance windowLength, int sampleCount, Velocity speedOfSound)
        {
            var duration = windowLength / speedOfSound;
            return 2 * (duration / sampleCount);
        }

        public static AcousticSettingsRaw MoveWindowStartOut(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation shortens the window range. Because we're maintaining a fixed number of samples,
            // we must do this by decreasing the sample period. The edge case is when we try to decrease the window
            // past its minimum sample period.

            if (settings.SamplePeriod <= cfg.RawConfiguration.SamplePeriodRange.Minimum)
            {
                return settings;
            }

            if (settings.SampleStartDelay >= cfg.RawConfiguration.SampleStartDelayRange.Maximum)
            {
                return settings;
            }

            var newSamplePeriod = settings.SamplePeriod - WindowTerminusAdjustment;
            var sampleTimeReduction = WindowTerminusAdjustment * settings.SampleCount;
            var newSampleStartDelay =
                (settings.SampleStartDelay + sampleTimeReduction)
                    .ConstrainTo(cfg.RawConfiguration.SampleStartDelayRange);

            return BuildNewWindowSettings(
                settings,
                newSampleStartDelay,
                newSamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusPosition: true);
        }

        public static AcousticSettingsRaw MoveWindowEndIn(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation shortens the window range. Because we're maintaining a fixed number of samples,
            // we must do this by decreasing the sample period. The edge case is when we try to decrease the window
            // past its minimum sample period.

            if (settings.SamplePeriod <= cfg.RawConfiguration.SamplePeriodRange.Minimum)
            {
                return settings;
            }

            var newSamplePeriod = settings.SamplePeriod - WindowTerminusAdjustment;

            return BuildNewWindowSettings(
                settings,
                settings.SampleStartDelay,
                newSamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusPosition: true);
        }

        public static AcousticSettingsRaw MoveWindowEndOut(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation extends the window range. Because we're maintaining a fixed number of samples,
            // we must do this by increasing the sample period. The edge case is when we try to enlarge the window
            // to start before minimum sample start delay.

            if (settings.SamplePeriod >= cfg.RawConfiguration.SamplePeriodRange.Maximum)
            {
                return settings;
            }

            var newSamplePeriod = settings.SamplePeriod + WindowTerminusAdjustment;

            return BuildNewWindowSettings(
                settings,
                settings.SampleStartDelay,
                newSamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusPosition: true);
        }

        public static AcousticSettingsRaw SlideRangeIn(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation slides the window range. Because we're maintaining a fixed number of samples and
            // a fixed sample period, we must do this by decreasing the sample start delay. The edge case is when
            // we try to reduce the sample start delay below the minimum.

            if (settings.SampleStartDelay <= cfg.RawConfiguration.SampleStartDelayRange.Minimum)
            {
                return settings;
            }

            var decrement = (settings.SampleCount * settings.SamplePeriod) / 3;
            var newSampleStartDelay = (settings.SampleStartDelay - decrement)
                .ConstrainTo(cfg.RawConfiguration.SampleStartDelayRange);

            return BuildNewWindowSettings(
                settings,
                newSampleStartDelay,
                settings.SamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusPosition: true);
        }

        public static AcousticSettingsRaw SlideRangeOut(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var cfg = SystemConfiguration.GetConfiguration(settings.SystemType);

            // This operation slides the window range. Because we're maintaining a fixed number of samples and
            // a fixed sample period, we must do this by increasing the sample start delay. The edge case is when
            // we try to increase the sample start delay above the maximum.

            if (settings.SampleStartDelay >= cfg.RawConfiguration.SampleStartDelayRange.Maximum)
            {
                return settings;
            }

            var increment = (settings.SampleCount * settings.SamplePeriod) / 3;
            var newSampleStartDelay = (settings.SampleStartDelay + increment)
                .ConstrainTo(cfg.RawConfiguration.SampleStartDelayRange);

            return BuildNewWindowSettings(
                settings,
                newSampleStartDelay,
                settings.SamplePeriod,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                automateFocusPosition: true);
        }

    }
}
