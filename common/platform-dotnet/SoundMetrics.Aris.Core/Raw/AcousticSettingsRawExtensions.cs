// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    internal static class AcousticSettingsRawExtensions
    {
        internal static Distance CalculateWindowStart(
            this AcousticSettingsRaw acousticSettings,
            Salinity salinity)
            => acousticSettings.SampleStartDelay
                * acousticSettings.ObservedConditions.SpeedOfSound(salinity) / 2;

        internal static Distance CalculateWindowLength(
            this AcousticSettingsRaw acousticSettings,
            Salinity salinity)
            => acousticSettings.SampleCount * acousticSettings.SamplePeriod
                * acousticSettings.ObservedConditions.SpeedOfSound(salinity) / 2;

        internal static FineDuration CalculateSampleStartDelay(
            this AcousticSettingsRaw acousticSettings,
            Salinity salinity)
        {
            var windowStart = CalculateWindowStart(acousticSettings, salinity);
            return 2 * (windowStart / acousticSettings.ObservedConditions.SpeedOfSound(salinity));
        }

        public static Velocity SpeedOfSound(
            this ObservedConditions observedConditions,
            Salinity salinity)
            =>
                Velocity.FromMetersPerSecond(
                    AcousticMath.CalculateSpeedOfSound(
                        observedConditions.WaterTemp,
                        observedConditions.Depth.Meters,
                        (double)salinity));
    }
}
