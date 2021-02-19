namespace SoundMetrics.Aris.Core.Raw
{
    internal static class AcousticSettingsOracleExtensions
    {
        public static AcousticSettingsRaw ApplyAllConstraints(this AcousticSettingsRaw settings)
            => AcousticSettingsOracle.ApplyAllConstraints(settings);
    }
}
