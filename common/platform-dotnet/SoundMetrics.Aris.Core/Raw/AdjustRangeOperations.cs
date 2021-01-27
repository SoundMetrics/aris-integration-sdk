﻿// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Core.Raw
{
    public delegate AcousticSettingsRaw AdjustRangeFn(AcousticSettingsRaw currentSettings);

    public static class AdjustRangeOperations
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

        public static AcousticSettingsRaw ToShortWindow(AcousticSettingsRaw currentSettings)
        {
            var sizingInfo = windowSizingInfo[currentSettings.SystemType];
            return ToFixedWindow(currentSettings, sizingInfo.SystemConfiguration, sizingInfo.FixedWindowSizeShort);
        }

        public static AcousticSettingsRaw ToMediumWindow(AcousticSettingsRaw currentSettings)
        {
            var sizingInfo = windowSizingInfo[currentSettings.SystemType];
            return ToFixedWindow(currentSettings, sizingInfo.SystemConfiguration, sizingInfo.FixedWindowSizeMedium);
        }

        public static AcousticSettingsRaw ToLongWindow(AcousticSettingsRaw currentSettings)
        {
            var sizingInfo = windowSizingInfo[currentSettings.SystemType];
            return ToFixedWindow(currentSettings, sizingInfo.SystemConfiguration, sizingInfo.FixedWindowSizeLong);
        }

        private static AcousticSettingsRaw ToFixedWindow(
            AcousticSettingsRaw currentSettings,
            SystemConfiguration systemConfiguration,
            in FixedWindowSize windowSize)
        {
            var sspd = currentSettings.SonarEnvironment.SpeedOfSound;

            var original = currentSettings;

            var sampleStartDelay =
                AcousticSettingsRaw.CalculateSampleStartDelay(windowSize.WindowStart, sspd)
                    .ConstrainTo(systemConfiguration.RawConfiguration.SampleStartDelayRange);

            var actualWindowStart = TimeToDistance(sampleStartDelay, sspd) / 2;

            var samplePeriod =
                CalculateSamplePeriod(actualWindowStart, windowSize.WindowEnd, original.SamplesPerBeam, sspd)
                    .ConstrainTo(systemConfiguration.RawConfiguration.SamplePeriodRange);

            return
                BuildNewSettings(
                    original,
                    sampleStartDelay,
                    samplePeriod,
                    original.AntiAliasing,
                    original.InterpacketDelay);
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
            AcousticSettingsRaw BuildNewSettings(
                AcousticSettingsRaw original,
                FineDuration sampleStartDelay,
                FineDuration samplePeriod,
                FineDuration antiAliasing,
                InterpacketDelaySettings interpacketDelay)
        {
            if (PingMode.TryGet((int)original.PingMode, out var pingMode))
            {
                var frameRate =
                    Rate.Min(
                        original.FrameRate,
                        MaxFrameRate.FindMaximumFrameRate(
                            original.SystemType,
                            pingMode,
                            original.SamplesPerBeam,
                            sampleStartDelay,
                            samplePeriod,
                            antiAliasing,
                            interpacketDelay));

                var pingsPerFrame = pingMode.PingsPerFrame;
                var framePeriod = 1 / frameRate;
                var cyclePeriod = framePeriod / pingsPerFrame;

                // ### Leaving these as-is for now.
                var pulseWidth = original.PulseWidth;
                var frequency = original.Frequency;
                var receiverGain = original.ReceiverGain;

                var speedOfSound = original.SonarEnvironment.SpeedOfSound;
                var windowStart = AcousticSettingsRaw.CalculateWindowStart(sampleStartDelay, speedOfSound);
                var windowLength = AcousticSettingsRaw.CalculateWindowLength(original.SamplesPerBeam, samplePeriod, speedOfSound);
                var midRangeFocus = windowStart + (windowLength / 2);

                var newSettings =
                    new AcousticSettingsRaw(
                        systemType: original.SystemType,
                        frameRate: frameRate,
                        samplesPerBeam: original.SamplesPerBeam,
                        sampleStartDelay: sampleStartDelay,
                        cyclePeriod: cyclePeriod,
                        samplePeriod: samplePeriod,
                        pulseWidth: pulseWidth,
                        pingMode: original.PingMode,
                        enableTransmit: original.EnableTransmit,
                        frequency: frequency,
                        enable150Volts: original.Enable150Volts,
                        receiverGain: receiverGain,
                        focusPosition: midRangeFocus,
                        antiAliasing: antiAliasing,
                        interpacketDelay: interpacketDelay,
                        sonarEnvironment: original.SonarEnvironment);

                if (original.SamplesPerBeam != newSettings.SamplesPerBeam)
                {
                    throw new ApplicationException(
                        $"sample count changed from [{original.SamplesPerBeam}] to [{newSettings.SamplesPerBeam}]");
                }

                return newSettings;
            }
            else
            {
                throw new ArgumentException($"Invalid ping mode: {original.PingMode}");
            }
        }

        private static readonly FineDuration WindowTerminusAdjustment = FineDuration.FromMicroseconds(2);

        public static AcousticSettingsRaw MoveWindowStartIn(AcousticSettingsRaw original)
        {
            var cfg = SystemConfiguration.GetConfiguration(original.SystemType);

            // This operation extends the window range. Because we're maintaining a fixed number of samples,
            // we must do this by increasing the sample period. The edge case is when we try to enlarge the window
            // to start before minimum sample start delay.

            if (original.SamplePeriod >= cfg.RawConfiguration.SamplePeriodRange.Maximum)
            {
                return original;
            }

            if (original.SampleStartDelay <= cfg.RawConfiguration.SampleStartDelayRange.Minimum)
            {
                return original;
            }

            var newSamplePeriod = original.SamplePeriod + WindowTerminusAdjustment;
            var additionalSampleTime = WindowTerminusAdjustment * original.SamplesPerBeam;
            var newSampleStartDelay =
                (original.SampleStartDelay - additionalSampleTime)
                    .ConstrainTo(cfg.RawConfiguration.SampleStartDelayRange);

            return BuildNewSettings(
                original,
                newSampleStartDelay,
                newSamplePeriod,
                original.AntiAliasing,
                original.InterpacketDelay);
        }

        public static FineDuration GetSamplePeriodToCover(Distance windowLength, int samplesPerBeam, Velocity speedOfSound)
        {
            var duration = windowLength / speedOfSound;
            return 2 * (duration / samplesPerBeam);
        }

        public static AcousticSettingsRaw MoveWindowStartOut(AcousticSettingsRaw original)
        {
            var cfg = SystemConfiguration.GetConfiguration(original.SystemType);

            // This operation shortens the window range. Because we're maintaining a fixed number of samples,
            // we must do this by decreasing the sample period. The edge case is when we try to decrease the window
            // past its minimum sample period.

            if (original.SamplePeriod <= cfg.RawConfiguration.SamplePeriodRange.Minimum)
            {
                return original;
            }

            if (original.SampleStartDelay >= cfg.RawConfiguration.SampleStartDelayRange.Maximum)
            {
                return original;
            }

            var newSamplePeriod = original.SamplePeriod - WindowTerminusAdjustment;
            var sampleTimeReduction = WindowTerminusAdjustment * original.SamplesPerBeam;
            var newSampleStartDelay =
                (original.SampleStartDelay + sampleTimeReduction)
                    .ConstrainTo(cfg.RawConfiguration.SampleStartDelayRange);

            return BuildNewSettings(
                original,
                newSampleStartDelay,
                newSamplePeriod,
                original.AntiAliasing,
                original.InterpacketDelay);
        }

        public static AcousticSettingsRaw MoveWindowEndIn(AcousticSettingsRaw original)
        {
            var cfg = SystemConfiguration.GetConfiguration(original.SystemType);

            // This operation shortens the window range. Because we're maintaining a fixed number of samples,
            // we must do this by decreasing the sample period. The edge case is when we try to decrease the window
            // past its minimum sample period.

            if (original.SamplePeriod <= cfg.RawConfiguration.SamplePeriodRange.Minimum)
            {
                return original;
            }

            var newSamplePeriod = original.SamplePeriod - WindowTerminusAdjustment;

            return BuildNewSettings(
                original,
                original.SampleStartDelay,
                newSamplePeriod,
                original.AntiAliasing,
                original.InterpacketDelay);
        }

        public static AcousticSettingsRaw MoveWindowEndOut(AcousticSettingsRaw original)
        {
            var cfg = SystemConfiguration.GetConfiguration(original.SystemType);

            // This operation extends the window range. Because we're maintaining a fixed number of samples,
            // we must do this by increasing the sample period. The edge case is when we try to enlarge the window
            // to start before minimum sample start delay.

            if (original.SamplePeriod >= cfg.RawConfiguration.SamplePeriodRange.Maximum)
            {
                return original;
            }

            var newSamplePeriod = original.SamplePeriod + WindowTerminusAdjustment;

            return BuildNewSettings(
                original,
                original.SampleStartDelay,
                newSamplePeriod,
                original.AntiAliasing,
                original.InterpacketDelay);
        }

        public static AcousticSettingsRaw SlideRangeIn(AcousticSettingsRaw original)
        {
            var cfg = SystemConfiguration.GetConfiguration(original.SystemType);

            // This operation slides the window range. Because we're maintaining a fixed number of samples and
            // a fixed sample period, we must do this by decreasing the sample start delay. The edge case is when
            // we try to reduce the sample start delay below the minimum.

            if (original.SampleStartDelay <= cfg.RawConfiguration.SampleStartDelayRange.Minimum)
            {
                return original;
            }

            var decrement = (original.SamplesPerBeam * original.SamplePeriod) / 3;
            var newSampleStartDelay = (original.SampleStartDelay - decrement)
                .ConstrainTo(cfg.RawConfiguration.SampleStartDelayRange);

            return BuildNewSettings(
                original,
                newSampleStartDelay,
                original.SamplePeriod,
                original.AntiAliasing,
                original.InterpacketDelay);
        }

        public static AcousticSettingsRaw SlideRangeOut(AcousticSettingsRaw original)
        {
            var cfg = SystemConfiguration.GetConfiguration(original.SystemType);

            // This operation slides the window range. Because we're maintaining a fixed number of samples and
            // a fixed sample period, we must do this by increasing the sample start delay. The edge case is when
            // we try to increase the sample start delay above the maximum.

            if (original.SampleStartDelay >= cfg.RawConfiguration.SampleStartDelayRange.Maximum)
            {
                return original;
            }

            var increment = (original.SamplesPerBeam * original.SamplePeriod) / 3;
            var newSampleStartDelay = (original.SampleStartDelay + increment)
                .ConstrainTo(cfg.RawConfiguration.SampleStartDelayRange);

            return BuildNewSettings(
                original,
                newSampleStartDelay,
                original.SamplePeriod,
                original.AntiAliasing,
                original.InterpacketDelay);
        }

    }
}
