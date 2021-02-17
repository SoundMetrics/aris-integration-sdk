// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    internal static class AcousticSettingsRawExtensions
    {
        internal static Distance CalculateWindowStart(
            this AcousticSettingsRaw acousticSettings)
            => acousticSettings.SampleStartDelay * acousticSettings.SonarEnvironment.SpeedOfSound / 2;

        internal static Distance CalculateWindowLength(
            this AcousticSettingsRaw acousticSettings)
            => acousticSettings.SampleCount * acousticSettings.SamplePeriod
                * acousticSettings.SonarEnvironment.SpeedOfSound / 2;

        internal static FineDuration CalculateSampleStartDelay(
            this AcousticSettingsRaw acousticSettings)
        {
            var windowStart = CalculateWindowStart(acousticSettings);
            return 2 * (windowStart / acousticSettings.SonarEnvironment.SpeedOfSound);
        }
    }
}
