// Copyright (c) 2010-2026 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core;

/// <summary>
/// These are extensions for observed conditions literally copied from
/// internal methods in SoundMetrics.Aris.Core.Raw.
/// </summary>
public static class ObservedConditionsExtensions
{
    public static FineDuration CalculateSampleStartDelay(
        Distance windowStart,
        Velocity speedOfSound)
    {
        return 2 * (windowStart / speedOfSound);
    }

    public static Distance ConvertSamplePeriodToResolution(
        this ObservedConditions observedConditions,
        FineDuration samplePeriod,
        Salinity salinity)
        => samplePeriod * observedConditions.SpeedOfSound(salinity) / 2;

    public static Velocity SpeedOfSound(
        this ObservedConditions observedConditions,
        Salinity salinity)
    {
        if (observedConditions is null)
        {
            throw new ArgumentNullException(nameof(observedConditions));
        }

        return
            Velocity.FromMetersPerSecond(
                AcousticMath.CalculateSpeedOfSound(
                    observedConditions.WaterTemp,
                    observedConditions.Depth,
                    (double)salinity));
    }
}
