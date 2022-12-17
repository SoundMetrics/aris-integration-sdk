// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsRaw_Aux;
    using static AcousticSettingsRawRangeOperations;
    using static MathSupport;

    internal sealed class AdjustWindowTerminusFree : IAdjustWindowTerminus
    {
        private AdjustWindowTerminusFree() { }

        public static readonly AdjustWindowTerminusFree Instance = new AdjustWindowTerminusFree();

        public AcousticSettingsRaw MoveWindowEnd(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedEnd,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            var windowBounds = settings.WindowBounds(observedConditions);
            var (windowStart, _, windowLength) = windowBounds;

            // rountrip time over the window
            var salinity = settings.Salinity;
            var newWindowRoughTimeOfFlight =
                2 * windowLength / observedConditions.SpeedOfSound(salinity);

            var autoFlags = GetAutoFlags(useAutoFrequency);
            var newSettings = MoveAllowingChangeToSampleCount();

            if (newSettings == settings)
            {
                return settings;
            }
            else
            {
                return
                    newSettings
                        .WithAutomaticSettings(observedConditions, autoFlags)
                        .WithMaxFrameRate(useMaxFrameRate)
                        .ApplyAllConstraints();
            }

            AcousticSettingsRaw MoveAllowingChangeToSampleCount()
            {
                var nominalSampleCount =
                    CalculateNominalSampleCount(
                        settings.SamplePeriod,
                        windowStart,
                        requestedEnd,
                        observedConditions.SpeedOfSound(settings.Salinity),
                        out var _);
                return settings.WithSampleCount(nominalSampleCount);
            }
        }

        public AcousticSettingsRaw MoveWindowStart(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            var windowBounds = settings.WindowBounds(observedConditions);
            var (windowStart, _, windowLength) = windowBounds;
            var sysCfg = settings.SystemType.GetConfiguration();

            if (windowStart <= requestedStart
                && settings.SamplePeriod <= sysCfg.RawConfiguration.SamplePeriodLimits.Minimum)
            {
                // Sample period is already at its minimum.
                return settings;
            }

            if (requestedStart <= windowStart
                && settings.SamplePeriod >= sysCfg.RawConfiguration.SamplePeriodLimits.Maximum)
            {
                // Sample period is already at its maximum.
                return settings;
            }

            // Make sure we don't move window end here.
            // Expand the window by adjusting sample period,
            // move the window start.

            var salinity = settings.Salinity;
            var newWindowRoughTimeOfFlight = 2 * windowLength / observedConditions.SpeedOfSound(salinity);
            var newSamplePeriod =
                (newWindowRoughTimeOfFlight / settings.SampleCount)
                    .RoundToMicroseconds()
                    .ConstrainTo(sysCfg.RawConfiguration.SamplePeriodLimits);

            if (newSamplePeriod == settings.SamplePeriod)
            {
                // Nothing to do.
                return settings;
            }

            // Calculate SSD backwards from WindowEnd -- by new [WindowEnd - (sample period * sample count)],
            // only do it in machine units (microseconds) to avoid conversion to/from distance
            // (distance is derived from the machine units).

            var newSampleStartDelay = CalculateNewSampleStartDelay();
            var autoFlags = GetAutoFlags(useAutoFrequency);

            return
                settings
                    .WithSamplePeriod(newSamplePeriod, useMaxFrameRate)
                    .WithSampleStartDelay(newSampleStartDelay, useMaxFrameRate)
                    .WithAutomaticSettings(observedConditions, autoFlags)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();

            FineDuration CalculateNewSampleStartDelay()
            {
                var oldSampleStartDelay = settings.SampleStartDelay;
                var oldSamplePeriod = settings.SamplePeriod;
                var oldWindowTimeOfFlight = settings.SampleCount * oldSamplePeriod;
                var newWindowTimeOfFlight = settings.SampleCount * newSamplePeriod;
                var calculatedEndWindowTime = oldSampleStartDelay + oldWindowTimeOfFlight;
                var calculatedSampleStartDelay = calculatedEndWindowTime - newWindowTimeOfFlight;
                return calculatedSampleStartDelay;
            }
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
                RawCalculations.FitSamplePeriodTo(constrainedWindowBounds, settings.SampleCount, sspd)
                    .ConstrainTo(sysCfg.RawConfiguration.SamplePeriodLimits);
            var sampleStartDelay =
                RawCalculations.CalculateSampleStartDelay(constrainedWindowBounds, sspd);

            return
                ChangeWindow.BuildNewWindowSettings(
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
                return RawCalculations.FitSampleCountTo(windowLength, samplePeriod, speedOfSound);
            }

            Distance CalculatedCorrectedWindowLength()
                => RawCalculations.CalculateWindowLength(sampleCount, samplePeriod, speedOfSound);
        }
    }
}
