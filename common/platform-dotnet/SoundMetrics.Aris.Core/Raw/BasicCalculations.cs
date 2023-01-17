// Copyright (c) 2010-2023 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core.Raw
{
    internal static class BasicCalculations
    {
        internal static Distance CalculateWindowStart(
            FineDuration sampleStartDelay,
            Salinity salinity,
            ObservedConditions observedConditions)
            => CalculateWindowStart(sampleStartDelay, observedConditions.SpeedOfSound(salinity));

        internal static Distance CalculateWindowStart(
            FineDuration sampleStartDelay,
            Velocity speedOfSound)
            => sampleStartDelay * speedOfSound / 2;

        internal static Distance CalculateWindowLength(
            int sampleCount,
            FineDuration samplePeriod,
            Salinity salinity,
            ObservedConditions observedConditions)
            => sampleCount * samplePeriod * observedConditions.SpeedOfSound(salinity) / 2;

        internal static Distance CalculateWindowLength(
            int sampleCount,
            FineDuration samplePeriod,
            Velocity speedOfSound)
            => samplePeriod * sampleCount * speedOfSound / 2;

        internal static Distance CalculateMinimumWindowLength(
            SystemConfiguration systemConfiguration,
            ObservedConditions observedConditions,
            Salinity salinity,
            SampleCountLimitType sampleCountLimits)
        {
            if (systemConfiguration is null) throw new ArgumentNullException(nameof(systemConfiguration));
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            return BasicCalculations.CalculateWindowLength(
                systemConfiguration.GetSampleCountLimit(sampleCountLimits).Minimum,
                systemConfiguration.RawConfiguration.SamplePeriodLimits.Minimum,
                observedConditions.SpeedOfSound(salinity));
        }

        internal static Distance CalculateMinimumWindowLength(
            SystemConfiguration systemConfiguration,
            ObservedConditions observedConditions,
            Salinity salinity,
            SampleCountLimitType sampleCountLimits,
            FineDuration samplePeriod)
        {
            if (observedConditions is null) throw new ArgumentNullException(nameof(observedConditions));

            return BasicCalculations.CalculateWindowLength(
                systemConfiguration.GetSampleCountLimit(sampleCountLimits).Minimum,
                samplePeriod,
                observedConditions.SpeedOfSound(salinity));
        }

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

        internal static FineDuration CalculateSampleStartDelay(
            in WindowBounds windowBounds,
            Velocity speedOfSound)
            => CalculateSampleStartDelay(windowBounds.WindowStart, speedOfSound);

        internal static int FitSampleCountTo(
            in WindowBounds windowBounds,
            FineDuration samplePeriod,
            Velocity speedOfSound)
            => FitSampleCountTo(windowBounds.WindowLength, samplePeriod, speedOfSound);

        internal static int FitSampleCountTo(
            Distance windowLength,
            FineDuration samplePeriod,
            Velocity speedOfSound)
            => (int)MathSupport.RoundAway(2 * windowLength / (samplePeriod * speedOfSound));

        internal static FineDuration FitSamplePeriodTo(
            in WindowBounds windowBounds,
            int sampleCount,
            Velocity speedOfSound)
        {
            // (2 * WL) / (N * SSPD)
            var samplePeriod = ((2 * windowBounds.WindowLength) / (sampleCount * speedOfSound));
            var roundedSamplePeriod = samplePeriod.RoundToMicroseconds();

            return roundedSamplePeriod;
        }
    }
}
