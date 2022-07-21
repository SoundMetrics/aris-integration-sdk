// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    internal static class AcousticSettingsRawCalculations
    {
        internal static Distance CalculateWindowStart(
            FineDuration sampleStartDelay,
            Salinity salinity,
            ObservedConditions observedConditions)
            => sampleStartDelay * observedConditions.SpeedOfSound(salinity) / 2;

        internal static Distance CalculateWindowLength(
            int sampleCount,
            FineDuration samplePeriod,
            Salinity salinity,
            ObservedConditions observedConditions)
            => sampleCount * samplePeriod * observedConditions.SpeedOfSound(salinity) / 2;

        internal static FineDuration CalculateSampleStartDelay(
            Distance windowStart,
            Salinity salinity,
            ObservedConditions observedConditions)
            => CalculateSampleStartDelay(
                    windowStart,
                    observedConditions.SpeedOfSound(salinity));

        internal static FineDuration CalculateSampleStartDelay(
            Distance windowStart,
            Velocity speedOfSound)
        {
            return 2 * windowStart / speedOfSound;
        }
    }
}
