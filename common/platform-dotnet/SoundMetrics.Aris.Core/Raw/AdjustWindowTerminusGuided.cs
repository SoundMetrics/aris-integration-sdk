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

            var minimumWindowLength =
                RawCalculations.CalculateMinimumWindowLength(settings, observedConditions);
            var minWindowStart = sysCfg.WindowStartLimits.Minimum;
            var maxWindowStart = windowEnd - minimumWindowLength;
            var constrainedStart = requestedStart.ConstrainTo((minWindowStart, maxWindowStart));
            var newWindowBounds = new WindowBounds(constrainedStart, windowEnd);

            Debug.Assert(newWindowBounds.WindowStart < newWindowBounds.WindowEnd);

            if ((constrainedStart - windowStart).Abs() <= MinimumSlideDisplacement)
            {
                return settings;
            }

            var newSettings =
                settings.CalculateSettingsWithGuidedSampleCount(
                    newWindowBounds,
                    observedConditions,
                    WindowPinning.PinToWindowEnd)
                    .WithMaxFrameRate(true)
                    .ApplyAllConstraints();

            if (settings.SamplePeriod != newSettings.SamplePeriod)
            {
                var windowEndA = windowEnd;
                var windowEndB = newSettings.WindowBounds(observedConditions).WindowEnd;
                Debug.WriteLine(
                    $"{nameof(MoveWindowStart)}: Sample period changed [{settings.SamplePeriod} -> {newSettings.SamplePeriod}]; "
                    + $"window end [{windowEndA} -> {windowEndB}]");
            }

            return newSettings;
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
            var newSettings =
                settings.CalculateSettingsWithGuidedSampleCount(
                    newWindowBounds,
                    observedConditions,
                    WindowPinning.PinToWindowStart)
                    .WithMaxFrameRate(true)
                    .ApplyAllConstraints();

            if (settings.SamplePeriod != newSettings.SamplePeriod)
            {
                Debug.WriteLine($"{nameof(MoveWindowEnd)}: Sample period changed [{settings.SamplePeriod} -> {newSettings.SamplePeriod}]");
            }

            return newSettings;

            Distance GetMinWindowEnd()
            {
                var minWindowLength =
                    RawCalculations.CalculateMinimumWindowLength(settings, observedConditions);
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
                    observedConditions,
                    WindowPinning.PinToWindowStart);

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
                    observedConditions,
                    WindowPinning.PinToWindowStart)
                    .WithMaxFrameRate(true)
                    .ApplyAllConstraints();
        }
    }
}
