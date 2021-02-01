// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    internal static class AcousticSettingsRawCalculations
    {
        internal static Distance CalculateWindowStart(
            FineDuration sampleStartDelay,
            EnvironmentalContext sonarEnvironment)
            => sampleStartDelay * sonarEnvironment.SpeedOfSound / 2;

        internal static Distance CalculateWindowLength(
            int samplesPerBeam,
            FineDuration samplePeriod,
            EnvironmentalContext sonarEnvironment)
            => samplesPerBeam * samplePeriod * sonarEnvironment.SpeedOfSound / 2;

        internal static FineDuration CalculateSampleStartDelay(
            Distance windowStart,
            EnvironmentalContext sonarEnvironment)
        {
            return 2 * (windowStart / sonarEnvironment.SpeedOfSound);
        }
    }
}
