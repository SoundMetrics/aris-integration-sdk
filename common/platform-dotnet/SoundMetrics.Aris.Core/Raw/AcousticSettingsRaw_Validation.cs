// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core.Raw
{
    public sealed partial class AcousticSettingsRaw
    {
        // Pass in the instance. At present, we don't have use of
        // init-only properties due to targeting .NET Standard 2.0.
        private static void CheckInvariants(AcousticSettingsRaw settings)
        {
            var sysCfg = settings.SystemType.GetConfiguration();
            var rawCfg = sysCfg.RawConfiguration;

            ValidateRange(
                nameof(settings.SamplePeriod),
                settings.SamplePeriod,
                rawCfg.SamplePeriodLimits);
        }

        private static void ValidateRange<TValue>(
            string valueName,
            in TValue value,
            in ValueRange<TValue> valueRange)
            where TValue : struct, IComparable<TValue>
        {
            if (!valueRange.Contains(value))
            {
                var errorMessage =
                    BuildRangeValidationErrorMessage(
                        valueName,
                        value,
                        valueRange);
                throw new ArgumentOutOfRangeException(errorMessage);
            }
        }

        private static string BuildRangeValidationErrorMessage<TValue>(
            string valueName,
            in TValue value,
            in ValueRange<TValue> valueRange)
            where TValue : struct, IComparable<TValue>
            =>
            $"Value '{valueName}' is [{value}]; this is not in range [{valueRange}]";
    }
}
