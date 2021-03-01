﻿using System;
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
            Distance requestedStart,
            bool useMaxFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (requestedStart <= Distance.Zero)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(nameof(requestedStart), "Value is negative or zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            // Plan: Don't change the sample count, just adjust the sample period.
            // Tactic: what integral sample period covers the smallest range that
            // encloses the requested window start without moving the end?

            var windowLength = settings.WindowEnd - requestedStart;
            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowStart)}: requested window length is lte zero");
                return settings;
            }

            var sysCfg = settings.SystemType.GetConfiguration();

            if (settings.WindowStart <= requestedStart
                && settings.SamplePeriod <= sysCfg.RawConfiguration.SamplePeriodRange.Minimum)
            {
                // Sample period is already at its minimum.
                return settings;
            }

            if (requestedStart <= settings.WindowStart
                && settings.SamplePeriod >= sysCfg.RawConfiguration.SamplePeriodRange.Maximum)
            {
                // Sample period is already at its maximum.
                return settings;
            }

            // Make sure we don't move window end here.
            // Expand the window by adjusting sample period,
            // move the window start.

            var newWindowRoughTimeOfFlight = 2 * windowLength / settings.SonarEnvironment.SpeedOfSound;
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

            var newSampleStartDelay = CalculatedNewSampleStartDelay();

            return
                settings
                    .WithSamplePeriod(newSamplePeriod, useMaxFrameRate)
                    .WithSampleStartDelay(newSampleStartDelay, useMaxFrameRate);

            FineDuration CalculatedNewSampleStartDelay()
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
                Distance requestedEnd,
                bool useMaxFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            if (requestedEnd <= Distance.Zero)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentOutOfRangeException(nameof(requestedEnd), "Value is negative or zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            // Plan: Don't change the sample count, just adjust the sample period.
            // Tactic: what integral sample period covers the smallest range that
            // encloses the requested window end?

            var windowLength = requestedEnd - settings.WindowStart;
            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowEnd)}: requested window length is lte zero");
                return settings;
            }

            // rountrip time over the window
            var timeOverWindow = 2 * windowLength / settings.SonarEnvironment.SpeedOfSound;
            var idealSamplePeriod = timeOverWindow / settings.SampleCount;
            var samplePeriod = idealSamplePeriod.Ceiling;

            var sysCfg = settings.SystemType.GetConfiguration();
            var constrainedSamplePeriod = samplePeriod.ConstrainTo(sysCfg.RawConfiguration.SamplePeriodRange);

            return settings.WithSamplePeriod(constrainedSamplePeriod, useMaxFrameRate);
        }
    }
}

