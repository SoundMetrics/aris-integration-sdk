// Copyright (c) 2022 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsRaw_Aux;
    using static AcousticSettingsRawCalculations;
    using static System.Math;

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
                    .DispatchOperation(
                        onFixed: SelectSpecificRange_Fixed,
                        onGuided: SelectSpecificRange_Guided,
                        onFree: SelectSpecificRange_Free);

            AcousticSettingsRaw SelectSpecificRange_Fixed()
                => CalculateSettingsWithFixedSampleCount(
                        settings,
                        constrainedWindowBounds,
                        observedConditions);

            AcousticSettingsRaw SelectSpecificRange_Guided()
                => CalculateSettingsWithGuidedSampleCount(
                        settings,
                        constrainedWindowBounds,
                        observedConditions);

            AcousticSettingsRaw SelectSpecificRange_Free()
                => CalculateFreeSettingsWithRange(
                        settings,
                        constrainedWindowBounds,
                        observedConditions,
                        useMaxFrameRate,
                        useAutoFrequency);
        }

        private static readonly Distance MinimumSlideDisplacement = (Distance)0.003;

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

            var (windowStart, windowEnd, windowLength) =
                settings.WindowBounds(observedConditions);

            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowStart)}: requested window length is lte zero");
                return settings;
            }

            return guidedSettingsMode
                    .DispatchOperation(
                        onFixed: MoveWindowStart_Fixed,
                        onGuided: MoveWindowStart_Guided,
                        onFree: MoveWindowStart_Free);

            AcousticSettingsRaw MoveWindowStart_Guided()
            {
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

            AcousticSettingsRaw MoveWindowStart_Fixed()
            {
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

            AcousticSettingsRaw MoveWindowStart_Free()
            {
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
            var (windowStart, windowEnd, windowLength) =
                settings.WindowBounds(observedConditions);

            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowEnd)}: requested window length is lte zero");
                return settings;
            }

            return guidedSettingsMode.DispatchOperation(
                onFixed: MoveWindowEnd_Fixed,
                onGuided: MoveWindowEnd_Guided,
                onFree: MoveWindowEnd_Free);

            AcousticSettingsRaw MoveWindowEnd_Guided()
            {
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

            AcousticSettingsRaw MoveWindowEnd_Fixed()
            {
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

            AcousticSettingsRaw MoveWindowEnd_Free()
            {
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
        }

        private static int CalculateNominalSampleCount(
            FineDuration samplePeriod,
            Distance windowStart,
            Distance windowEnd,
            Velocity speedOfSound,
            out Distance correctedWindowEnd)
        {
            double sampleCount = CalculateSampleCount();
            correctedWindowEnd = windowStart + CalculatedCorrectedWindowLength();
            return (int)sampleCount;

            double CalculateSampleCount()
            {
                /*
                 * samplePeriod = 2 * windowLength / (sspd * sampleCount)
                 *
                 * samplePeriod * sampleCount = 2 * windowLength / sspd
                 *
                 * sampleCount = 2 * windowLength / (sspd * samplePeriod)
                 *
                 * [round]
                 */
                var windowLength = windowEnd - windowStart;
                return Math.Round(
                    2 * windowLength / (speedOfSound * samplePeriod));
            }

            Distance CalculatedCorrectedWindowLength()
            {
                /*
                 * samplePeriod = 2 * windowLength / (sspd * sampleCount)
                 *
                 *      so
                 *
                 * windowLength = samplePeriod * sspd * sampleCount / 2
                 */
                return samplePeriod * speedOfSound * sampleCount / 2.0;
            }
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

            var originalBounds = settings.WindowBounds(observedConditions);
            var sysCfg = settings.SystemType.GetConfiguration();

            var newSettings = guidedSettingsMode
                    .DispatchOperation(
                        onFixed: SlideWindow_Fixed,
                        onGuided: SlideWindow_Guided,
                        onFree: SlideWindow_Free);

            return newSettings;

            AcousticSettingsRaw SlideWindow_Guided()
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

            AcousticSettingsRaw SlideWindow_Fixed()
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
                    settings.CalculateSettingsWithFixedSampleCount(
                        newWindowBounds,
                        observedConditions)
                        .WithMaxFrameRate(useMaxFrameRate)
                        .ApplyAllConstraints();
            }

            AcousticSettingsRaw SlideWindow_Free()
            {
                // Don't adjust the sample count, just adjust the sample start delay.

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
        }

        private static WindowBounds GetConstrainedWindowBounds(
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
            var (windowStart, windowEnd) = windowBounds;
            var waterTemperature = observedConditions.WaterTemp;

            var frequency =
                CalculateFrequencyPerWindowEnd(
                    systemType, waterTemperature, salinity, windowEnd);
            var sspd = observedConditions.SpeedOfSound(salinity);

            var pulseWidth =
                CalculateAutoPulseWidth(
                    systemType,
                    waterTemperature,
                    salinity,
                    windowEnd);
            var samplePeriod =
                CalculateAutoSamplePeriod(
                    systemType,
                    waterTemperature,
                    windowEnd);
            var sampleCount =
                CalculateGuidedSamplesPerBeam(
                    systemType,
                    windowBounds,
                    sspd,
                    samplePeriod,
                    out var _);
            var sampleStartDelay = CalculateSampleStartDelay(windowStart, sspd);

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
            var integralSampleCount = (int)Round(sampleCount);
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
                return samplePeriodPerSampleCount;
            }
        }

        internal static AcousticSettingsRaw
            CalculateFreeSettingsWithRange(
                this AcousticSettingsRaw settings,
                in WindowBounds requestedWindow,
                ObservedConditions observedConditions,
                bool useMaxFrameRate,
                bool useAutoFrequency)
        {
            var sysCfg = settings.SystemType.GetConfiguration();
            var constrainedWindowBounds =
                GetConstrainedWindowBounds(sysCfg, requestedWindow);

            var (windowStart, windowEnd) = constrainedWindowBounds;
            var sspd = observedConditions.SpeedOfSound(settings.Salinity);

            var samplePeriod =
                CalculateSamplePeriod(windowStart, windowEnd, settings.SampleCount, sspd)
                    .ConstrainTo(sysCfg.RawConfiguration.SamplePeriodLimits);
            var sampleStartDelay = 2 * windowStart / sspd;

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
