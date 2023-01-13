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
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedEnd,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            // Rationale: in this mode we are free to change sample count, so
            // hold sample period (resolution) constant and adjust sample count
            // to fit the new window (within limits). The user controls the
            // sample period.

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var desiredWindowBounds = new WindowBounds(windowBounds.WindowStart, requestedEnd);
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
        }

        public AcousticSettingsRaw MoveWindowStart(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            // Rationale: in this mode we are free to change sample count, so
            // hold sample period (resolution) constant and adjust sample count
            // to fit the new window (within limits). The user controls the
            // sample period.

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var desiredWindowBounds = new WindowBounds(requestedStart, windowBounds.WindowEnd);
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
        }

        public AcousticSettingsRaw SelectSpecificRange(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds windowBounds,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => CalculateFreeSettingsWithRange(
                    settings,
                    windowBounds,
                    observedConditions,
                    useMaxFrameRate,
                    useAutoFrequency);

        public AcousticSettingsRaw SlideWindow(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedStart,
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
