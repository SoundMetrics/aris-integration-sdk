// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    using System;
    using System.Diagnostics;
    using static AcousticSettingsRawRangeOperations;

    public sealed class AdjustWindowTerminusLevel2 : IAdjustWindowTerminus
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
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            // Rationale: in this mode we are free to change sample count, so
            // hold sample period (resolution) constant and adjust sample count
            // to fit the new window (within limits). The user controls the
            // sample period.

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var constrainedWindowEnd = ConstrainWindowEnd(requestedEnd);
            var desiredWindowBounds = new WindowBounds(windowBounds.WindowStart, constrainedWindowEnd);

            var (sampleCountFittedToWindow, samplePeriod)
                = FitSampleCountSafely(
                    desiredWindowBounds,
                    sysCfg,
                    settings.SamplePeriod,
                    observedConditions.SpeedOfSound(settings.Salinity));

            Debug.WriteLine(
                $"{nameof(MoveWindowEnd)}: [{windowBounds.ToShortString()} -> {desiredWindowBounds.ToShortString()}]; "
                + $"{nameof(sampleCountFittedToWindow)}=[{sampleCountFittedToWindow}]");

            var withSampleCountAndPeriod =
                settings
                    .WithSampleCount(sampleCountFittedToWindow)
                    .WithSamplePeriod(samplePeriod, useMaxFrameRate);
#pragma warning disable CA1062 // Validate arguments of public methods - spurious compile error even though we're checking 'settings' for null
            var newWindowEnd = withSampleCountAndPeriod.WindowBounds(observedConditions).WindowEnd;
#pragma warning restore CA1062 // Validate arguments of public methods

            var newSettings =
                withSampleCountAndPeriod
                    .WithFrequency(
                        sysCfg.SelectFrequency(
                            observedConditions.WaterTemp,
                            settings.Salinity,
                            newWindowEnd,
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

        private static (int SampleCount, FineDuration SamplePeriod)
            FitSampleCountSafely(
                in WindowBounds windowBounds,
                SystemConfiguration sysCfg,
                FineDuration samplePeriod,
                Velocity sspd)
        {
            int naiveSampleCount = FitNaively(windowBounds, samplePeriod);
            var sampleCountDeviceLimits = sysCfg.SampleCountDeviceLimits;

            int newSampleCount;
            FineDuration newSamplePeriod;

            if (sampleCountDeviceLimits.Contains(naiveSampleCount))
            {
                newSampleCount = naiveSampleCount;
                newSamplePeriod = samplePeriod;
            }
            else
            {
                Debug.WriteLine(
                    $"{nameof(FitSampleCountSafely)}: sample count [{naiveSampleCount}] is not in [{sampleCountDeviceLimits}]");

                if (naiveSampleCount < sampleCountDeviceLimits.Minimum)
                {
                    newSampleCount = sampleCountDeviceLimits.Minimum;
                    newSamplePeriod = BasicCalculations.FitSamplePeriodTo(windowBounds, newSampleCount, sspd);
                }
                else
                {
                    Debug.Assert(naiveSampleCount > sampleCountDeviceLimits.Maximum);

                    // Determine the minimum sample period necessary to bring the sample
                    // count down to what is allowable.
                    newSamplePeriod =
                        BasicCalculations.FitSamplePeriodTo(windowBounds, sampleCountDeviceLimits.Maximum, sspd);
                    newSampleCount = FitNaively(windowBounds, newSamplePeriod);

                    Debug.Assert(sysCfg.RawConfiguration.SamplePeriodLimits.Contains(newSamplePeriod));
                    Debug.Assert(sampleCountDeviceLimits.Contains(newSampleCount));
                    Debug.Assert(newSampleCount != naiveSampleCount);
                    Debug.Assert(newSamplePeriod != samplePeriod);
                    Debug.Assert(sysCfg.RawConfiguration.SamplePeriodLimits.Contains(newSamplePeriod));
                }
            }

            Debug.WriteLine(
                $"{nameof(FitSampleCountSafely)}: new values: sample count=[{newSampleCount}]; sample period=[{newSamplePeriod}]");

            return (newSampleCount, newSamplePeriod);

            int FitNaively(in WindowBounds wb, FineDuration sp) // pass wb due to C# 7.3 constraints CS1628
                => BasicCalculations.FitSampleCountTo(wb, sp, sspd);
        }

        public AcousticSettingsRaw MoveWindowStart(
            Distance requestedStart,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

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
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            return CalculateFreeSettingsWithRange(
                    settings,
                    windowBounds,
                    observedConditions,
                    useMaxFrameRate,
                    useAutoFrequency);
        }

        public AcousticSettingsRaw SlideWindow(
            Distance requestedStart,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

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
