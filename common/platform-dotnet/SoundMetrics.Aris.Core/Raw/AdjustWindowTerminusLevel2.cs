// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    using System.Diagnostics;
    using static AcousticSettingsRawRangeOperations;

    internal sealed class AdjustWindowTerminusLevel2 : IAdjustWindowTerminus
    {
        private AdjustWindowTerminusLevel2() { }

        public static readonly AdjustWindowTerminusLevel2 Instance = new AdjustWindowTerminusLevel2();

        public AcousticSettingsRaw MoveWindowEnd(
            Distance requestedEnd,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            // Rationale: in this mode we are free to change sample count, so
            // hold sample period (resolution) constant and adjust sample count
            // to fit the new window (within limits). The user controls the
            // sample period.

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var constrainedWindowEnd = ConstrainWindowEnd(requestedEnd);
            var desiredWindowBounds = new WindowBounds(windowBounds.WindowStart, constrainedWindowEnd);

            var sampleCountFittedToWindow =
                BasicCalculations.FitSampleCountTo(
                    desiredWindowBounds,
                    settings.SamplePeriod,
                    observedConditions.SpeedOfSound(settings.Salinity));

            Debug.WriteLine(
                $"{nameof(MoveWindowEnd)}: [{windowBounds.ToShortString()} -> {desiredWindowBounds.ToShortString()}]; "
                + $"{nameof(sampleCountFittedToWindow)}=[{sampleCountFittedToWindow}]");

            var newSettings =
                settings
                    .WithSampleCount(sampleCountFittedToWindow)
                    .WithFrequency(
                        sysCfg.SelectFrequency(
                            observedConditions.WaterTemp,
                            settings.Salinity,
                            desiredWindowBounds.WindowEnd,
                            useAutoFrequency,
                            fallbackValue: settings.Frequency))
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            return newSettings;

            Distance ConstrainWindowEnd(Distance windowEnd)
            {
                var minimumWindowLength =
                    BasicCalculations.CalculateMinimumWindowLength(
                        sysCfg,
                        observedConditions,
                        settings.Salinity,
                        SampleCountLimitType.Device,
                        settings.SamplePeriod);

                var validWindowEndRange = (windowBounds.WindowStart + minimumWindowLength, sysCfg.WindowEndLimits.Maximum);
                Debug.Assert(validWindowEndRange.Item1 <= validWindowEndRange.Item2);
                return windowEnd.ConstrainTo(validWindowEndRange);
            }
        }

        public AcousticSettingsRaw MoveWindowStart(
            Distance requestedStart,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            // Rationale: in this mode we are free to change sample count, so
            // hold sample period (resolution) constant and adjust sample count
            // to fit the new window (within limits). The user controls the
            // sample period.

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var constrainedWindowStart = ConstrainWindowStart(requestedStart);
            var desiredWindowBounds = new WindowBounds(constrainedWindowStart, windowBounds.WindowEnd);
            var sampleCountFittedToWindow =
                BasicCalculations.FitSampleCountTo(
                    desiredWindowBounds,
                    settings.SamplePeriod,
                    observedConditions.SpeedOfSound(settings.Salinity));

            Debug.WriteLine(
                $"{nameof(MoveWindowStart)}: [{windowBounds.ToShortString()} -> {desiredWindowBounds.ToShortString()}]; "
                + $"{nameof(sampleCountFittedToWindow)}=[{sampleCountFittedToWindow}]");

            var newSettings =
                settings
                    .WithSampleCount(sampleCountFittedToWindow)
                    .PinWindow(
                        WindowPinning.PinToWindowEnd,
                        desiredWindowBounds,
                        observedConditions,
                        settings.Salinity)
                    .WithFrequency(
                        sysCfg.SelectFrequency(
                            observedConditions.WaterTemp,
                            settings.Salinity,
                            desiredWindowBounds.WindowEnd,
                            useAutoFrequency,
                            fallbackValue: settings.Frequency))
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            return newSettings;

            Distance ConstrainWindowStart(Distance windowStart)
            {
                var minimumWindowLength =
                    BasicCalculations.CalculateMinimumWindowLength(
                        sysCfg,
                        observedConditions,
                        settings.Salinity,
                        SampleCountLimitType.Device,
                        settings.SamplePeriod);

                var validWindowStartRange = (sysCfg.WindowStartLimits.Minimum, windowBounds.WindowEnd - minimumWindowLength);
                Debug.Assert(validWindowStartRange.Item1 <= validWindowStartRange.Item2);
                return windowStart.ConstrainTo(validWindowStartRange);
            }
        }

        public AcousticSettingsRaw SelectSpecificRange(
            WindowBounds windowBounds,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => CalculateFreeSettingsWithRange(
                    settings,
                    windowBounds,
                    observedConditions,
                    useMaxFrameRate,
                    useAutoFrequency);

        public AcousticSettingsRaw SlideWindow(
            Distance requestedStart,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            // Don't adjust the sample count, just adjust the sample start delay.

            var sysCfg = settings.SystemType.GetConfiguration();
            var originalBounds = settings.WindowBounds(observedConditions);
            var windowStart = originalBounds.WindowStart;

            if (requestedStart == windowStart)
            {
                return settings;
            }

            var salinity = settings.Salinity;
            var newSampleStartDelay =
                (2 * requestedStart / observedConditions.SpeedOfSound(salinity))
                    .ConstrainTo(sysCfg.RawConfiguration.SampleStartDelayLimits);
            var autoFlags = GetAutoFlags(useAutoFrequency);
            return
                settings
                    .WithSampleStartDelay(newSampleStartDelay, useMaxFrameRate)
                    .WithAutomaticSettings(observedConditions, autoFlags)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }

        internal static AcousticSettingsRaw
            CalculateFreeSettingsWithRange(
                AcousticSettingsRaw settings,
                in WindowBounds requestedWindow,
                ObservedConditions observedConditions,
                bool useMaxFrameRate,
                bool useAutoFrequency)
        {
            var sysCfg = settings.SystemType.GetConfiguration();
            var constrainedWindowBounds =
                GetConstrainedWindowBounds(sysCfg, requestedWindow);

            var sspd = observedConditions.SpeedOfSound(settings.Salinity);

            var samplePeriod =
                BasicCalculations.FitSamplePeriodTo(constrainedWindowBounds, settings.SampleCount, sspd)
                    .ConstrainTo(sysCfg.RawConfiguration.SamplePeriodLimits);
            var sampleStartDelay =
                BasicCalculations.CalculateSampleStartDelay(constrainedWindowBounds, sspd);

            return
                PredefinedWindowSizes.BuildNewWindowSettings(
                    settings,
                    observedConditions,
                    sampleStartDelay,
                    samplePeriod,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    automateFocusDistance: true,
                    useMaxFrameRate,
                    useAutoFrequency);
        }

        private static int CalculateNominalSampleCount(
            FineDuration samplePeriod,
            Distance windowStart,
            Distance windowEnd,
            Velocity speedOfSound,
            out Distance correctedWindowEnd)
        {
            var sampleCount = CalculateSampleCount();
            correctedWindowEnd = windowStart + CalculatedCorrectedWindowLength();
            return sampleCount;

            int CalculateSampleCount()
            {
                var windowLength = windowEnd - windowStart;
                return BasicCalculations.FitSampleCountTo(windowLength, samplePeriod, speedOfSound);
            }

            Distance CalculatedCorrectedWindowLength()
                => BasicCalculations.CalculateWindowLength(sampleCount, samplePeriod, speedOfSound);
        }
    }
}
