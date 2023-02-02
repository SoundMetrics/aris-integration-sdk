// Copyright (c) 2022-2023 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using static AcousticSettingsRawRangeOperations;
    using static Distance;

    public sealed class PreferredGuidedSampleCounts
    {
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
#pragma warning disable CA1822 // Member this[] does not access instance data and can be marked as static
        public ValueRange<int> this[SystemType systemType] => sampleCountLimitMap[systemType];
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
#pragma warning restore CA1822

        private static IReadOnlyDictionary<SystemType, ValueRange<int>> GenerateSampleCountLimitMap()
        {
            var maxSampleCount = 4000;
            Debug.Assert(maxSampleCount == SystemType.Aris3000.GetConfiguration().SampleCountDeviceLimits.Maximum);

            return
                new Dictionary<SystemType, ValueRange<int>>()
                {
                    { SystemType.Aris1200, new ValueRange<int>(1750, maxSampleCount) },
                    { SystemType.Aris1800, new ValueRange<int>(1250, maxSampleCount) },
                    { SystemType.Aris3000, new ValueRange<int>(800, maxSampleCount) },
                };
        }

        private static readonly IReadOnlyDictionary<SystemType, ValueRange<int>>
            sampleCountLimitMap = GenerateSampleCountLimitMap();
    }

    public sealed class AdjustWindowTerminusGuided : IAdjustWindowTerminus
    {
        private AdjustWindowTerminusGuided() { }

        public static readonly AdjustWindowTerminusGuided Instance = new AdjustWindowTerminusGuided();

        /// <summary>
        /// These are preferred limits, not device hardware limits.
        /// </summary>
        public static PreferredGuidedSampleCounts SampleCountLimits { get; } = new PreferredGuidedSampleCounts();

        AcousticSettingsRaw IAdjustWindowTerminus.MoveWindowStart(
            Distance requestedStart,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            var sampleCountLimits = SampleCountLimits[settings.SystemType];

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var (windowStart, windowEnd) = windowBounds;

            var minimumWindowLength =
                BasicCalculations.CalculateMinimumWindowLength(
                    sysCfg,
                    observedConditions,
                    settings.Salinity,
                    sampleCountLimits);
            var minWindowStart = sysCfg.WindowLimits.Minimum;
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
                    $"{nameof(IAdjustWindowTerminus.MoveWindowStart)}: Sample period changed [{settings.SamplePeriod} -> {newSettings.SamplePeriod}]; "
                    + $"window end [{windowEndA} -> {windowEndB}]");
            }

            return newSettings;
        }

        AcousticSettingsRaw IAdjustWindowTerminus.MoveWindowEnd(
            Distance requestedEnd,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            var sampleCountLimits = SampleCountLimits[settings.SystemType];

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var (windowStart, windowEnd) = windowBounds;

            var constrainedEnd =
                Max(requestedEnd.ConstrainTo(sysCfg.WindowLimits),
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
                Debug.WriteLine($"{nameof(IAdjustWindowTerminus.MoveWindowEnd)}: Sample period changed [{settings.SamplePeriod} -> {newSettings.SamplePeriod}]");
            }

            return newSettings;

            Distance GetMinWindowEnd()
            {
                var minWindowLength =
                    BasicCalculations.CalculateMinimumWindowLength(
                        sysCfg,
                        observedConditions,
                        settings.Salinity,
                        sampleCountLimits);
                return windowBounds.WindowStart + minWindowLength;
            }
        }

        AcousticSettingsRaw IAdjustWindowTerminus.SelectSpecificRange(
            WindowBounds windowBounds,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool _useMaxFrameRate,
            bool _useAutoFrequency)
            => settings.CalculateSettingsWithGuidedSampleCount(
                    windowBounds,
                    observedConditions,
                    WindowPinning.PinToWindowStart);

        AcousticSettingsRaw IAdjustWindowTerminus.SlideWindow(
            Distance requestedStart,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

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
