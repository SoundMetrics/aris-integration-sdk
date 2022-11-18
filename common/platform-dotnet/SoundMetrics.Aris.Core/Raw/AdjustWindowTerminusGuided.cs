// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    using System.Diagnostics;
    using static AcousticSettingsRaw_Aux;
    using static AcousticSettingsRawRangeOperations;
    using static Distance;

    internal sealed class AdjustWindowTerminusGuided : IAdjustWindowTerminus
    {
        private AdjustWindowTerminusGuided() { }

        public static readonly AdjustWindowTerminusGuided Instance = new AdjustWindowTerminusGuided();

        public AcousticSettingsRaw MoveWindowStart(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var (windowStart, windowEnd) = windowBounds;

            var minimumWindowLength = CalculateMinimumWindowLength(settings, observedConditions);
            var minWindowStart = sysCfg.WindowStartLimits.Minimum;
            var maxWindowStart = windowEnd - minimumWindowLength;
            var constrainedStart = Max(Min(requestedStart, maxWindowStart), minWindowStart);
            var newWindowBounds = new WindowBounds(constrainedStart, windowEnd);

            Debug.Assert(newWindowBounds.WindowStart < newWindowBounds.WindowEnd);

            if ((constrainedStart - windowStart).Abs() <= MinimumSlideDisplacement)
            {
                return settings;
            }

            return SelectSpecificRange(settings, observedConditions, newWindowBounds, useMaxFrameRate, useAutoFrequency);
        }

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

            var constrainedEnd =
                Max(requestedEnd.ConstrainTo(sysCfg.WindowEndLimits),
                    GetMinWindowEnd());

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

            Distance GetMinWindowEnd()
            {
                var minWindowLength = CalculateMinimumWindowLength(settings, observedConditions);
                return windowBounds.WindowStart + minWindowLength;
            }
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
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency)
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
                settings.CalculateSettingsWithGuidedSampleCount(
                    newWindowBounds,
                    observedConditions)
                    .WithMaxFrameRate(true)
                    .ApplyAllConstraints();
        }
    }
}
