// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundMetrics.Aris.Core.Raw
{
    public sealed partial class AcousticSettingsRaw
    {
        /// Enumerates the differences between two instances of
        /// AcousticSettingsRaw.
        public static IEnumerable<(string PropertyName, object Left, object Right)>
            GetDifferences(AcousticSettingsRaw left, AcousticSettingsRaw right)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (object.Equals(left, right))
            {
                return Array.Empty<(string PropertyName, object Left, object Right)>();
            }
            else
            {
                return EnumeratePropertyDifferences();
            }

            IEnumerable<(string PropertyName, object Left, object Right)>
                EnumeratePropertyDifferences()
            {
                return new List<(string PropertyName, object Left, object Right)?>
                {
                    GetDifferences(nameof(left.SystemType), left.SystemType, right.SystemType),
                    GetDifferences(nameof(left.FrameRate), left.FrameRate, right.FrameRate),
                    GetDifferences(nameof(left.SampleCount), left.SampleCount, right.SampleCount),
                    GetDifferences(nameof(left.SampleStartDelay), left.SampleStartDelay, right.SampleStartDelay),
                    GetDifferences(nameof(left.SamplePeriod), left.SamplePeriod, right.SamplePeriod),
                    GetDifferences(nameof(left.PulseWidth), left.PulseWidth, right.PulseWidth),
                    GetDifferences(nameof(left.PingMode), left.PingMode, right.PingMode),
                    GetDifferences(nameof(left.EnableTransmit), left.EnableTransmit, right.EnableTransmit),
                    GetDifferences(nameof(left.Frequency), left.Frequency, right.Frequency),
                    GetDifferences(nameof(left.Enable150Volts), left.Enable150Volts, right.Enable150Volts),
                    GetDifferences(nameof(left.ReceiverGain), left.ReceiverGain, right.ReceiverGain),
                    GetDifferences(nameof(left.FocusDistance), left.FocusDistance, right.FocusDistance),
                    GetDifferences(nameof(left.AntiAliasing), left.AntiAliasing, right.AntiAliasing),
                    GetDifferences(nameof(left.InterpacketDelay), left.InterpacketDelay, right.InterpacketDelay),
                    GetDifferences(nameof(left.Salinity), left.Salinity, right.Salinity),
                }
                .Where(el => el.HasValue)
                .Select(el => el.Value);
            }

            (string PropertyName, object Left, object Right)?
                GetDifferences<T>(string propertyname, in T l, in T r)
            {
                if (object.Equals(l, r))
                {
                    return null;
                }
                else
                {
                    return (PropertyName: propertyname, Left: l, Right: r);
                }
            }
        }

        public static IEnumerable<string>
            GetFormattedDifferences(AcousticSettingsRaw left, AcousticSettingsRaw right)
            => GetDifferences(left, right)
                .Select(FormatPropertyDifference);

        public static string
            GetAllDifferencesFormatted(AcousticSettingsRaw left, AcousticSettingsRaw right)
            => string.Join("; ", GetFormattedDifferences(left, right));

        private static string FormatPropertyDifference(
            (string PropertyName, object Left, object Right) t)
            => $"{t.PropertyName}=[{t.Left} -> {t.Right}]";
    }
}
