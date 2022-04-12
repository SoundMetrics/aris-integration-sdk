// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsOracle;

    public static class AcousticSettingsRawExtensions
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

        internal static Distance ConvertSamplePeriodToResolution(
            this ObservedConditions observedConditions,
            FineDuration samplePeriod,
            Salinity salinity)
            => samplePeriod * observedConditions.SpeedOfSound(salinity) / 2;

        internal static Velocity SpeedOfSound(
            this ObservedConditions observedConditions,
            Salinity salinity)
            =>
                Velocity.FromMetersPerSecond(
                    AcousticMath.CalculateSpeedOfSound(
                        observedConditions.WaterTemp,
                        observedConditions.Depth,
                        (double)salinity));

        //---------------------------------------------------------------------
        // Logging support

        [ThreadStatic]
        private static (bool IsEnabled, int Count) settingsChangeLogging;

        internal static (bool IsEnabled, int Count) SettingsChangeLogging
        {
            get => settingsChangeLogging;
            private set => settingsChangeLogging = value;
        }

        /// <summary>
        /// Enables settings logging on the current thread. Nested calls have
        /// undefined behavior.
        /// </summary>
        /// <param name="enable">Enables logging for the scope of the return.</param>
        /// <returns>Disposable that terminates the logging scope when disposed.</returns>
        public static IDisposable EnableSettingsChangeLoggingOnThread(
            string context,
            bool enable)
        {
            if (enable)
            {
                if (SettingsChangeLogging.IsEnabled)
                {
                    LogSettingsChangeContext($"Settings change logging is already enabled in {context}");
                }
                else
                {
                    SettingsChangeLogging =
                        (IsEnabled: true, Count: SettingsChangeLogging.Count + 1);

                    LogSettingsChangeContext($"Enabled settings tracing in {context}");
                }

                return new CleanUpLogging();
            }
            else
            {
                return NoopCleanUpLogging;
            }
        }

        private static readonly NoopCleanUp NoopCleanUpLogging = new NoopCleanUp();

        private sealed class NoopCleanUp : IDisposable
        {
            public void Dispose() { }
        }

        private sealed class CleanUpLogging : IDisposable
        {
            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    SettingsChangeLogging =
                        (IsEnabled: false, Count: SettingsChangeLogging.Count);
                }
            }

            private bool disposed;
        }

        //---------------------------------------------------------------------
        // Diff support

        internal static bool GetDifferences(
            AcousticSettingsRaw a,
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

        internal static bool GetDifferences(
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
