// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace SoundMetrics.Aris.Core.Raw
{
    public static class AcousticSettingsRawExtensions
    {
        internal static FineDuration CalculateSampleStartDelay(
            this AcousticSettingsRaw acousticSettings,
            ObservedConditions observedConditions,
            Salinity salinity)
        {
            var windowStart = acousticSettings.WindowBounds(observedConditions).WindowStart;
            var speedOfSound = observedConditions.SpeedOfSound(salinity);
            return CalculateSampleStartDelay(windowStart, speedOfSound);
        }

        internal static FineDuration CalculateSampleStartDelay(
            Distance windowStart,
            Velocity speedOfSound)
        {
            return 2 * (windowStart / speedOfSound);
        }

        internal static Distance ConvertSamplePeriodToResolution(
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

        internal static AcousticSettingsRaw CopyRawWith(
            this AcousticSettingsRaw settings,
            Salinity? salinity = null,
            Frequency? frequency = null,
            FineDuration? sampleStartDelay = null,
            FineDuration? samplePeriod = null,
            int? sampleCount = null,
            FineDuration? pulseWidth = null)
        {
            var newSettings = new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    sampleCount ?? settings.SampleCount,
                    sampleStartDelay ?? settings.SampleStartDelay,
                    samplePeriod ?? settings.SamplePeriod,
                    pulseWidth ?? settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    frequency ?? settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    salinity ?? settings.Salinity);

            // Prefer keeping the old settings if they're unchanged
            // (for easier equality/comparison).
            return settings == newSettings ? settings : newSettings;
        }

        //---------------------------------------------------------------------
        // Diff support

        internal static bool GetDifference(
            this AcousticSettingsRaw a,
            AcousticSettingsRaw b,
            out string differences)
        {
            if (a is null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b is null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            var buf = new StringBuilder();
            var isDifferent = GetDifferences(a, b, buf);
            differences = buf.ToString();

            return isDifferent;
        }

        private static bool GetDifferences(
            AcousticSettingsRaw a,
            AcousticSettingsRaw b,
            StringBuilder differences)
        {
            if (a is null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b is null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            if (differences is null)
            {
                throw new ArgumentNullException(nameof(differences));
            }

            bool isDifferent = false;
            bool isFirst = true;

            foreach (var propertyInfo in PublicPropertyInfos)
            {
                isDifferent = isDifferent | ReportDifference(propertyInfo);
                if (isDifferent)
                {
                    isFirst = false;
                }
            }

            return isDifferent;

            bool ReportDifference(PropertyInfo propertyInfo)
            {
                var valueA = propertyInfo.GetValue(a);
                var valueB = propertyInfo.GetValue(b);

                if (object.Equals(valueA, valueB))
                {
                    Debug.WriteLine($"{propertyInfo.Name} unchanged at {GetInvariantFormatttedString(valueA)}");
                    return false;
                }
                else
                {
                    if (!isFirst)
                    {
                        _ = differences.Append("; ");
                    }

                    var valueStringA = GetInvariantFormatttedString(valueA);
                    var valueStringB = GetInvariantFormatttedString(valueB);
                    var difference = $"{propertyInfo.Name} [{valueStringA}]->[{valueStringB}]";

                    _ = differences.Append(difference);

                    return true;
                }
            }
        }

        // Gets an invariant formatted version of the value.
        internal static string GetInvariantFormatttedString(object value)
            => value is null
                ? "(null)"
                : string.Format(CultureInfo.InvariantCulture, "{0}", value);

        private static IReadOnlyList<PropertyInfo> PublicPropertyInfos =
            GetPropertyInfos<AcousticSettingsRaw>();

        private static PropertyInfo[] GetPropertyInfos<T>()
        {
            var flags = BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance;
            return typeof(T).GetProperties(flags);
        }

    }
}
