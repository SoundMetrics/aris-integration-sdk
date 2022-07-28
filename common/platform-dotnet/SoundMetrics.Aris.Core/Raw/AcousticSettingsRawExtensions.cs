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
            return 2 * (windowStart / observedConditions.SpeedOfSound(salinity));
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

        //---------------------------------------------------------------------
        // Logging support

        [ThreadStatic]
        private static (int Count, int ScopeDepth) settingsChangeLogging;

        internal static int SettingsChangeLoggingCounter => settingsChangeLogging.Count;

        internal static bool IsSettingsChangeLoggingEnabled => settingsChangeLogging.ScopeDepth > 0;

        /// <summary>
        /// Enables settings logging on the current thread. Nested calls have
        /// undefined behavior.
        /// </summary>
        /// <param name="enable">Enables logging for the scope of the return.</param>
        /// <returns>Disposable that terminates the logging scope when disposed.</returns>
        public static IDisposable StartSettingsChangeLoggingScope(
            string context,
            bool enable)
        {
            if (enable)
            {
                var (count, scopeDepth) = settingsChangeLogging;

                var firstEntryIntoScope = scopeDepth == 0;
                var incrementedScopeDepth = scopeDepth + 1;

                count = firstEntryIntoScope ? count + 1 : count;

                settingsChangeLogging = (Count: count, ScopeDepth: incrementedScopeDepth);
                var indent = new string('>', incrementedScopeDepth);

                AcousticSettingsOracle.LogSettingsContext($"Enabled settings tracing in {indent} {context}");

                return new CleanUpLogging();
            }
            else
            {
                return NoopCleanUpLogging;
            }
        }

        private static void ExitSettingsLogging()
        {
            var (count, scopeDepth) = settingsChangeLogging;
            var decrementedScopeDepth = scopeDepth - 1;

            if (decrementedScopeDepth < 0)
            {
                throw new InvalidOperationException($"{nameof(decrementedScopeDepth)} is invalid: [{decrementedScopeDepth}]");
            }

            settingsChangeLogging = (Count: count, ScopeDepth: decrementedScopeDepth);
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
                    ExitSettingsLogging();
                }
            }

            private bool disposed;
        }

        internal static AcousticSettingsRaw LogSettingsContext(
            this AcousticSettingsRaw settings,
            string context)
        {
            if (IsSettingsChangeLoggingEnabled)
            {
                Trace.TraceInformation(context);
            }

            return settings;
        }

        internal static AcousticSettingsRaw LogSettingsContext(
            this AcousticSettingsRaw settings,
            string prefix,
            Func<AcousticSettingsRaw, string> buildContext)
        {
            if (IsSettingsChangeLoggingEnabled)
            {
                var context = prefix + " " + buildContext(settings);
                return settings.LogSettingsContext(context);
            }
            else
            {
                return settings;
            }
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
