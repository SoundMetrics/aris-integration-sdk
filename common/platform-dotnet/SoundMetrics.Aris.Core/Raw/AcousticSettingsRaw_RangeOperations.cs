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
            // encloses the requested window end?

            var windowLength = settings.WindowEnd - requestedStart;
            if (windowLength <= Distance.Zero)
            {
                Trace.TraceError($"{nameof(MoveWindowStart)}: requested window length is lte zero");
                return settings;
            }

            // rountrip time over the window
            var timeOverWindow = 2 * windowLength / settings.SonarEnvironment.SpeedOfSound;
            var idealSamplePeriod = timeOverWindow / settings.SampleCount;
            var samplePeriod = idealSamplePeriod.Ceiling;

            var sysCfg = settings.SystemType.GetConfiguration();
            var constrainedSamplePeriod = samplePeriod.ConstrainTo(sysCfg.RawConfiguration.SamplePeriodRange);

            var sampleStartDelay = 2 * requestedStart / settings.SonarEnvironment.SpeedOfSound;

            return settings
                .WithSampleStartDelay(sampleStartDelay, useMaxFrameRate)
                .WithSamplePeriod(constrainedSamplePeriod, useMaxFrameRate);
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

