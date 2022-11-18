// Copyright (c) 2022 Sound Metrics Corp.

using System;
using System.Diagnostics;
using static SoundMetrics.Aris.Core.MathSupport;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsRaw_Aux;
    using static AcousticSettingsRawCalculations;

    public static class AcousticSettingsRawRangeOperations
    {
        public static AcousticSettingsRaw GetSettingsForSpecificRange(
            this AcousticSettingsRaw settings,
            GuidedSettingsMode guidedSettingsMode,
            ObservedConditions observedConditions,
            in WindowBounds requestedWindow,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            var (windowStart, windowEnd) = requestedWindow;

            if (windowStart <= Distance.Zero)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(
                    nameof(requestedWindow),
                    $"Value of {nameof(requestedWindow.WindowStart)} is negative or zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var sysCfg = settings.SystemType.GetConfiguration();
            var constrainedWindowBounds =
                GetConstrainedWindowBounds(sysCfg, requestedWindow);

            return guidedSettingsMode
                    .GetAdjustWindowOperations()
                    .SelectSpecificRange(
                        settings,
                        observedConditions,
                        constrainedWindowBounds,
                        useMaxFrameRate,
                        useAutoFrequency);
        }

        internal static readonly Distance MinimumSlideDisplacement = (Distance)0.003;

        /// <summary>
        /// Moves the range start, attempting to enclose the requested distance.
        /// </summary>
        public static AcousticSettingsRaw MoveWindowStart(
            this AcousticSettingsRaw settings,
            GuidedSettingsMode guidedSettingsMode,
            ObservedConditions observedConditions,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            if (requestedStart <= Distance.Zero)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(nameof(requestedStart), "Value is negative or zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            // Plan: Don't change the sample count, just adjust the sample period.
            // Tactic: what integral sample period covers the smallest range that
            // encloses the requested window start without moving the end?

            var windowBounds = settings.WindowBounds(observedConditions);
            var (_, _, windowLength) = windowBounds;

            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowStart)}: requested window length is lte zero");
                return settings;
            }

            Debug.WriteLine($"### @@@ ({nameof(MoveWindowStart)}) guidedSettingsMode=[{guidedSettingsMode}]");
            return guidedSettingsMode
                    .GetAdjustWindowOperations()
                    .MoveWindowStart(
                        settings,
                        observedConditions,
                        requestedStart,
                        useMaxFrameRate,
                        useAutoFrequency);
        }

        /// <summary>
        /// Moves the range end, attempting to enclose the requested distance.
        /// </summary>
        public static AcousticSettingsRaw MoveWindowEnd(
                this AcousticSettingsRaw settings,
                GuidedSettingsMode guidedSettingsMode,
                ObservedConditions observedConditions,
                Distance requestedEnd,
                bool useMaxFrameRate,
                bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            if (requestedEnd <= Distance.Zero)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(nameof(requestedEnd), "Value is negative or zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            // Plan: Don't change the sample count, just adjust the sample period.
            // Tactic: what integral sample period covers the smallest range that
            // encloses the requested window end?

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowBounds = settings.WindowBounds(observedConditions);
            var (_, _, windowLength) = windowBounds;

            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowEnd)}: requested window length is lte zero");
                return settings;
            }

            return guidedSettingsMode
                    .GetAdjustWindowOperations()
                    .MoveWindowEnd(
                        settings,
                        observedConditions,
                        requestedEnd,
                        useMaxFrameRate,
                        useAutoFrequency);
        }

        internal static (Distance MinStart, Distance MaxStart)
            GetStartMinMax(
                AcousticSettingsRaw settings,
                ObservedConditions observedConditions)
        {
            var sysCfg = settings.SystemType.GetConfiguration();

            var windowLength = settings.WindowBounds(observedConditions).WindowLength;
            var minWindowStart = sysCfg.WindowStartLimits.Minimum;
            var maxWindowStart = sysCfg.WindowEndLimits.Maximum - windowLength;
            return (minWindowStart, maxWindowStart);
        }

        internal static AutomaticAcousticSettings GetAutoFlags(
            bool useAutoFrequency)
        {
            var autoFlags = AutomaticAcousticSettings.None;
            autoFlags |= AutomaticAcousticSettings.FocusDistance;
            autoFlags |= AutomaticAcousticSettings.PulseWidth;
            autoFlags |= useAutoFrequency ? AutomaticAcousticSettings.Frequency : AutomaticAcousticSettings.None;
            return autoFlags;
        }

        public static AcousticSettingsRaw MoveEntireWindow(
                this AcousticSettingsRaw settings,
                GuidedSettingsMode guidedSettingsMode,
                ObservedConditions observedConditions,
                Distance requestedStart,
                bool useMaxFrameRate,
                bool useAutoFrequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            if (requestedStart <= Distance.Zero)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(nameof(requestedStart), "Value is negative or zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var newSettings =
                guidedSettingsMode
                    .GetAdjustWindowOperations()
                    .SlideWindow(
                        settings,
                        observedConditions,
                        requestedStart,
                        useMaxFrameRate,
                        useAutoFrequency);

            return newSettings;
        }

        internal static WindowBounds GetConstrainedWindowBounds(
            SystemConfiguration sysCfg,
            in WindowBounds windowBounds)
        {
            var (windowStart, windowEnd) = windowBounds;

            return new WindowBounds(
                windowStart.ConstrainTo(sysCfg.WindowStartLimits),
                windowEnd.ConstrainTo(sysCfg.WindowEndLimits));
        }

        public static AcousticSettingsRaw
            CreateDefaultGuidedSettings(
                SystemType systemType,
                Salinity salinity,
                in WindowBounds windowBounds,
                ObservedConditions observedConditions)
        {
            return
                systemType
                    .GetConfiguration()
                    .GetDefaultSettings(observedConditions, salinity)
                    .CalculateSettingsWithGuidedSampleCount(
                        windowBounds, observedConditions)
                    .WithMaxFrameRate(enable: true);
        }

        public static AcousticSettingsRaw
            CalculateSettingsWithGuidedSampleCount(
                this AcousticSettingsRaw settings,
                in WindowBounds windowBounds,
                ObservedConditions observedConditions)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (observedConditions is null)
            {
                throw new ArgumentNullException(nameof(observedConditions));
            }

            /* Per spec ["sample count" is used in code rather than "samples per beam"].
                When Not Recording
                1. Calculate Frequency Crossover Range  (Range Xover) from System Type, Temperature, Salinity
                2. Calculate Frequency (HF when Range End <= Range Xover, LF when Range End > Range Xover)
                3. Calculate Pulse Width from Range Xover, Range End, System Type
                4. Calculate Sample Period from Range End, System Type and Temperature
                5. Calculate SamplesPerBeam from Sample Period, Range Start, Range End and constrain if necessary
                6. Calculate StartSampleDelay from Range Start, Sound Velocity
            */

            var systemType = settings.SystemType;
            var salinity = settings.Salinity;
            var waterTemperature = observedConditions.WaterTemp;
            var sspd = observedConditions.SpeedOfSound(salinity);

            // Calculate sample start delay first and adjust the requested bounds
            // to use the new value.
            var (sampleStartDelay, adjustedBounds) =
                CalculateWindowBounds(windowBounds, sspd);

            var frequency =
                CalculateFrequencyPerWindowEnd(
                    systemType,
                    waterTemperature,
                    salinity,
                    adjustedBounds.WindowEnd);

            var pulseWidth =
                CalculateAutoPulseWidth(
                    systemType,
                    waterTemperature,
                    salinity,
                    adjustedBounds.WindowEnd);
            var samplePeriod =
                CalculateAutoSamplePeriod(
                    systemType,
                    waterTemperature,
                    adjustedBounds.WindowEnd);
            var sampleCount =
                CalculateGuidedSamplesPerBeam(
                    systemType,
                    adjustedBounds,
                    sspd,
                    samplePeriod,
                    out var _);

            var newRawValues =
                settings.CopyRawWith(
                    frequency: frequency,
                    sampleStartDelay: sampleStartDelay,
                    sampleCount: sampleCount,
                    samplePeriod: samplePeriod,
                    pulseWidth: pulseWidth);

            var constrained = newRawValues.ApplyAllConstraints();
            return constrained;
        }

        private static
            (FineDuration sampleStartDelay, WindowBounds adjustedWindowBounds)
            CalculateWindowBounds(
                in WindowBounds bounds,
                Velocity speedOfSound)
        {
            // Also use sample start delay to get proper distance for
            // the window start.

            var ssd = CalculateSampleStartDelay(bounds.WindowStart, speedOfSound);
            var adjustedWindowStart = CalculateWindowStart(ssd, speedOfSound);
            var realBounds = bounds.MoveStartTo(adjustedWindowStart);

            return (ssd, realBounds);
        }

        private static int CalculateGuidedSamplesPerBeam(
            SystemType systemType,
            in WindowBounds windowBounds,
            Velocity sspd,
            FineDuration samplePeriod,
            out Distance adjustedRangeEnd)
        {
            /* Per spec ["sample count" is used in code rather than "samples per beam"].
                SamplesPerBeam Calculated from Sample Period, Range Start, Range End
                1.  SamplesPerBeam = (Range End - Range Start) / (SSPD/2 * Sample Period)
                2.  Constrain SamplesPerBeam = max(SamplesPerBeam, Min SPBper System Type)
                3.  Adjust Range End = Range Start + SamplesPerBeam * SSPD/2 * Sample Period
             */

            var (windowStart, _, windowLength) = windowBounds;

            var sampleCount = windowLength / (sspd / 2 * samplePeriod);
            var integralSampleCount = (int)RoundAway(sampleCount);
            var systemPreferredLimits =
                systemType.GetConfiguration().SampleCountPreferredLimits;
            var constrainedSampleCount =
                integralSampleCount.ConstrainTo(systemPreferredLimits);

            // Likely unnecessary, and solely advisory;
            // range end is defined by sample period and sample count.
            adjustedRangeEnd = windowStart + CalculateWindowLength(samplePeriod, integralSampleCount, sspd);

            return constrainedSampleCount;
        }

        internal static AcousticSettingsRaw
            CalculateSettingsWithFixedSampleCount(
                this AcousticSettingsRaw settings,
                in WindowBounds windowBounds,
                ObservedConditions observedConditions)
        {
            /* Per spec
                When Manual Recording
                1. On Range Start or End change calculate Range Xover, Frequency, Pulse Width
                2. SamplesPerBeam is fixed at the value when recording was initiated
                3. Calculate new Sample Period from Range End, Range Start and constrain if necessary
                4. Adjust Range Start or Range End from Sample Period
                5. Calculate StartSampleDelay from Range Start, Sound Velocity
            */

            var systemType = settings.SystemType;
            var salinity = settings.Salinity;
            var sampleCount = settings.SampleCount;
            var (windowStart, windowEnd) = windowBounds;
            var waterTemperature = observedConditions.WaterTemp;
            var sysCfg = systemType.GetConfiguration();
            var rawCfg = sysCfg.RawConfiguration;

            var frequency =
                CalculateFrequencyPerWindowEnd(
                    systemType, waterTemperature, salinity, windowEnd);
            var sspd = observedConditions.SpeedOfSound(salinity);

            var samplePeriod = FitSamplePeriodPerSampleCount();
            var sampleStartDelay = CalculateSampleStartDelay(windowStart, sspd);

            var correctedWindowEnd =
                CalculateWindowEnd(windowStart, samplePeriod, sspd, sampleCount);

            var pulseWidth =
                CalculateAutoPulseWidth(
                    systemType,
                    waterTemperature,
                    salinity,
                    correctedWindowEnd);

            var newRawValues =
                settings.CopyRawWith(
                    frequency: frequency,
                    sampleStartDelay: sampleStartDelay,
                    samplePeriod: samplePeriod,
                    pulseWidth: pulseWidth);

            var constrained = newRawValues.ApplyAllConstraints();

            Debug.Assert(constrained.SampleCount == settings.SampleCount);

            return constrained;

            FineDuration FitSamplePeriodPerSampleCount()
            {
                var windowlength = windowEnd - windowStart;
                var samplePeriodPerSampleCount = (2 * windowlength / (sspd * sampleCount)).RoundToMicroseconds();

                var constrainedSamplePeriod =
                    samplePeriodPerSampleCount.ConstrainTo(rawCfg.SamplePeriodLimits);
                return constrainedSamplePeriod;
            }
        }

        private static Distance CalculateWindowEnd(
            Distance windowStart,
            FineDuration samplePeriod,
            Velocity sspd,
            int sampleCount)
        {
            var newWindowLength = samplePeriod * sspd * sampleCount / 2;
            var windowEnd = windowStart + newWindowLength;
            return windowEnd;

        }
    }
}
