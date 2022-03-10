using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core.Raw
{
    public static class AcousticSettingsRawRangeOperations
    {
        /// <summary>
        /// Moves the range start, attempting to enclose the requested distance.
        /// </summary>
        public static AcousticSettingsRaw MoveWindowStart(
            this AcousticSettingsRaw settings,
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

            var windowStart = settings.WindowStart(observedConditions);
            var windowEnd = settings.WindowEnd(observedConditions);

            var windowLength = windowEnd - requestedStart;
            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowStart)}: requested window length is lte zero");
                return settings;
            }

            var sysCfg = settings.SystemType.GetConfiguration();

            if (windowStart <= requestedStart
                && settings.SamplePeriod <= sysCfg.RawConfiguration.SamplePeriodRange.Minimum)
            {
                // Sample period is already at its minimum.
                return settings;
            }

            if (requestedStart <= windowStart
                && settings.SamplePeriod >= sysCfg.RawConfiguration.SamplePeriodRange.Maximum)
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
                    .ConstrainTo(sysCfg.RawConfiguration.SamplePeriodRange);

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

        /// <summary>
        /// Moves the range end, attempting to enclose the requested distance.
        /// </summary>
        public static AcousticSettingsRaw MoveWindowEnd(
                this AcousticSettingsRaw settings,
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

            var windowStart = settings.WindowStart(observedConditions);
            var windowLength = requestedEnd - windowStart;
            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowEnd)}: requested window length is lte zero");
                return settings;
            }

            var sysCfg = settings.SystemType.GetConfiguration();

            // rountrip time over the window
            var salinity = settings.Salinity;
            var newWindowRoughTimeOfFlight = 2 * windowLength / observedConditions.SpeedOfSound(salinity);
            var newSamplePeriod =
                (newWindowRoughTimeOfFlight / settings.SampleCount)
                    .RoundToMicroseconds()
                    .ConstrainTo(sysCfg.RawConfiguration.SamplePeriodRange);

            if (newSamplePeriod == settings.SamplePeriod)
            {
                // Nothing to do.
                return settings;
            }

            var autoFlags = GetAutoFlags(useAutoFrequency);
            return
                settings
                    .WithSamplePeriod(newSamplePeriod, useMaxFrameRate)
                    .WithAutomaticSettings(observedConditions, autoFlags)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }

        private static AutomaticAcousticSettings GetAutoFlags(
            bool useAutoFrequency)
        {
            var autoFlags = AutomaticAcousticSettings.None;
            autoFlags |= AutomaticAcousticSettings.FocusPosition;
            autoFlags |= AutomaticAcousticSettings.PulseWidth;
            autoFlags |= useAutoFrequency ? AutomaticAcousticSettings.Frequency : AutomaticAcousticSettings.None;
            return autoFlags;
        }

        public static AcousticSettingsRaw SlideWindow(
                this AcousticSettingsRaw settings,
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

            // Plan: Don't change the sample count, just adjust the sampel start delay.

            var sysCfg = settings.SystemType.GetConfiguration();
            var windowStart = settings.WindowStart(observedConditions);

            if (requestedStart == windowStart)
            {
                return settings;
            }

            var salinity = settings.Salinity;
            var newSampleStartDelay =
                (2 * requestedStart / observedConditions.SpeedOfSound(salinity))
                    .ConstrainTo(sysCfg.RawConfiguration.SampleStartDelayRange);
            var autoFlags = GetAutoFlags(useAutoFrequency);
            return
                settings
                    .WithSampleStartDelay(newSampleStartDelay, useMaxFrameRate)
                    .WithAutomaticSettings(observedConditions, autoFlags)
                    .WithMaxFrameRate(useMaxFrameRate)
                    .ApplyAllConstraints();
        }
    }
}
