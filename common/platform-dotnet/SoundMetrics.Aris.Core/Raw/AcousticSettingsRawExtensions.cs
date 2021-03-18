// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    internal static class AcousticSettingsRawExtensions
    {
        internal static Distance CalculateWindowStart(
            this AcousticSettingsRaw acousticSettings,
            ObservedConditions observedConditions,
            Salinity salinity)
            => acousticSettings.SampleStartDelay
                * observedConditions.SpeedOfSound(salinity) / 2;

        internal static Distance CalculateWindowLength(
            this AcousticSettingsRaw acousticSettings,
            ObservedConditions observedConditions,
            Salinity salinity)
            => acousticSettings.SampleCount * acousticSettings.SamplePeriod
                * observedConditions.SpeedOfSound(salinity) / 2;

        internal static FineDuration CalculateSampleStartDelay(
            this AcousticSettingsRaw acousticSettings,
            ObservedConditions observedConditions,
            Salinity salinity)
        {
            var windowStart = CalculateWindowStart(acousticSettings, observedConditions, salinity);
            return 2 * (windowStart / observedConditions.SpeedOfSound(salinity));
        }

        public static Velocity SpeedOfSound(
            this ObservedConditions observedConditions,
            Salinity salinity)
            =>
                Velocity.FromMetersPerSecond(
                    AcousticMath.CalculateSpeedOfSound(
                        observedConditions.WaterTemp,
                        observedConditions.Depth,
                        (double)salinity));
    }
}
