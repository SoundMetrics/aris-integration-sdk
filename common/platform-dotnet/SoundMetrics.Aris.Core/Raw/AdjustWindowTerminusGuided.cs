// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsRawRangeOperations;

    internal sealed class AdjustWindowTerminusGuided : IAdjustWindowTerminus
    {
        private AdjustWindowTerminusGuided() { }

        public static readonly AdjustWindowTerminusGuided Instance = new AdjustWindowTerminusGuided();

        public AcousticSettingsRaw MoveWindowStart(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds windowBounds,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            var (windowStart, windowEnd) = windowBounds;
            var constrainedStart =
                requestedStart.ConstrainTo(
                    GetStartMinMax(settings, observedConditions));
            if ((constrainedStart - windowStart).Abs() <= MinimumSlideDisplacement)
            {
                return settings;
            }

            var newWindowBounds = new WindowBounds(constrainedStart, windowEnd);
            return
                settings.CalculateSettingsWithGuidedSampleCount(
                    newWindowBounds,
                    observedConditions)
                    .WithMaxFrameRate(true)
                    .ApplyAllConstraints();
        }

        public AcousticSettingsRaw MoveWindowEnd(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds windowBounds,
            Distance requestedEnd,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            var sysCfg = settings.SystemType.GetConfiguration();
            var (windowStart, windowEnd) = windowBounds;

            var constrainedEnd = requestedEnd.ConstrainTo(sysCfg.WindowEndLimits);
            if ((constrainedEnd - windowEnd).Abs() <= MinimumSlideDisplacement)
            {
                return settings;
            }

            var newWindowBounds = new WindowBounds(windowStart, constrainedEnd);
            return
                settings.CalculateSettingsWithGuidedSampleCount(
                    newWindowBounds,
                    observedConditions)
                    .WithMaxFrameRate(true)
                    .ApplyAllConstraints();
        }

        public AcousticSettingsRaw SelectSpecificRange(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds windowBounds,
            bool _useMaxFrameRate,
            bool _useAutoFrequency)
            => settings.CalculateSettingsWithGuidedSampleCount(
                    windowBounds,
                    observedConditions);

        public AcousticSettingsRaw SlideWindow(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds originalBounds,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            var constrainedStart =
                requestedStart.ConstrainTo(
                    GetStartMinMax(settings, observedConditions));
            if ((constrainedStart - originalBounds.WindowStart).Abs() <= MinimumSlideDisplacement)
            {
                return settings;
            }

            var newWindowBounds =
                new WindowBounds(constrainedStart, constrainedStart + originalBounds.WindowLength);

            return
                settings.CalculateSettingsWithGuidedSampleCount(
                    newWindowBounds,
                    observedConditions)
                    .WithMaxFrameRate(true)
                    .ApplyAllConstraints();
        }
    }
}
