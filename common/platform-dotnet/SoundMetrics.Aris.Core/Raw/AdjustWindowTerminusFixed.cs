// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsRawRangeOperations;

    internal sealed class AdjustWindowTerminusFixed : IAdjustWindowTerminus
    {
        private AdjustWindowTerminusFixed() { }

        public static readonly AdjustWindowTerminusFixed Instance = new AdjustWindowTerminusFixed();

        public AcousticSettingsRaw MoveWindowEnd(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedEnd,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var (windowStart, windowEnd) = windowBounds;
            var constrainedEnd = requestedEnd.ConstrainTo(sysCfg.WindowEndLimits);
            if ((constrainedEnd - windowEnd).Abs() <= MinimumSlideDisplacement)
            {
                return settings;
            }

            var newWindowBounds = new WindowBounds(windowStart, constrainedEnd);

            return
                settings.CalculateSettingsWithFixedSampleCount(
                    newWindowBounds,
                    observedConditions)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }

        public AcousticSettingsRaw MoveWindowStart(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool _useAutoFrequency)
        {
            var windowBounds = settings.WindowBounds(observedConditions);
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
                settings.CalculateSettingsWithFixedSampleCount(
                    newWindowBounds,
                    observedConditions)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }

        public AcousticSettingsRaw SelectSpecificRange(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds windowBounds,
            bool useMaxFrameRate,
            bool useAutoFrequency)
            => settings.CalculateSettingsWithFixedSampleCount(
                        windowBounds,
                        observedConditions);

        public AcousticSettingsRaw SlideWindow(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool _useAutoFrequency)
        {
            var originalBounds = settings.WindowBounds(observedConditions);
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
                settings.CalculateSettingsWithFixedSampleCount(
                    newWindowBounds,
                    observedConditions)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }
    }
}
