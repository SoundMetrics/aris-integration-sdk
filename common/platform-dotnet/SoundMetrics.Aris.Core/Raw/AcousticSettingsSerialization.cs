using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SoundMetrics.Aris.Core.Raw
{
    using PropertyMap = Dictionary<string, Func<string, object>>;

    /// <summary>
    /// Serialization methods used for parsing log files and
    /// constructing unit tests.
    /// </summary>
    public static class AcousticSettingsSerialization
    {
        public static string Serialize(this AcousticSettingsRaw settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // E.g.,
            // SystemType=[ARIS 3000]; FrameRate=[1]; SampleCount=[1200]; SampleStartDelay=[930]; SamplePeriod=[4];
            //  PulseWidth=[6]; PingMode=[9]; EnableTransmit=[True]; Frequency=[High]; Enable150Volts=[True];
            //  ReceiverGain=[0]; FocusDistance=[8.000 m]; AntiAliasing=[0]; InterpacketDelay=[False,0]

            return
                  $"{nameof(AcousticSettingsRaw.SystemType)}=[{Serialize(settings.SystemType)}]; "
                + $"{nameof(AcousticSettingsRaw.FrameRate)}=[{settings.FrameRate.Hz}]; "
                + $"{nameof(AcousticSettingsRaw.SampleCount)}=[{settings.SampleCount}]; "
                + $"{nameof(AcousticSettingsRaw.SampleStartDelay)}=[{settings.SampleStartDelay.TotalMicroseconds}]; "
                + $"{nameof(AcousticSettingsRaw.SamplePeriod)}=[{settings.SamplePeriod.TotalMicroseconds}]; "
                + $"{nameof(AcousticSettingsRaw.PulseWidth)}=[{settings.PulseWidth.TotalMicroseconds}]; "
                + $"{nameof(AcousticSettingsRaw.PingMode)}=[{settings.PingMode.IntegralValue}]; "
                + $"{nameof(AcousticSettingsRaw.EnableTransmit)}=[{settings.EnableTransmit}]; "
                + $"{nameof(AcousticSettingsRaw.Frequency)}=[{settings.Frequency}]; "
                + $"{nameof(AcousticSettingsRaw.Enable150Volts)}=[{settings.Enable150Volts}]; "
                + $"{nameof(AcousticSettingsRaw.ReceiverGain)}=[{settings.ReceiverGain}]; "
                + $"{nameof(AcousticSettingsRaw.FocusDistance)}=[{Serialize(settings.FocusDistance)}]; "
                + $"{nameof(AcousticSettingsRaw.AntiAliasing)}=[{settings.AntiAliasing.TotalMicroseconds}]; "
                + $"{nameof(AcousticSettingsRaw.InterpacketDelay)}=[{Serialize(settings.InterpacketDelay)}]; "
                + $"{nameof(AcousticSettingsRaw.Salinity)}=[{settings.Salinity}]";
        }

        private static string Serialize(in SystemType systemType) => systemType.HumanReadableString;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "Policy")]
        private static string Serialize(in FocusDistance focusDistance)
        {
            if (focusDistance.Distance.HasValue)
            {
                return $"{focusDistance.Distance.Value}";
            }
            else if (focusDistance.FocusUnits.HasValue)
            {
                return $"{focusDistance.FocusUnits.Value}";
            }
            else
            {
                throw new ArgumentException($"Invalid focus distance", nameof(focusDistance));
            }
        }

        private static string Serialize(in InterpacketDelaySettings interpacketDelaySettings)
            => $"{interpacketDelaySettings.Enable},{interpacketDelaySettings.Delay.TotalMicroseconds}";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "Policy")]
        public static AcousticSettingsRaw Deserialize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentException($"'{nameof(s)}' cannot be null or whitespace.", nameof(s));
            }

            var settings = SettingsBuilder.Build(s);
            return settings;
        }


        private static class SettingsBuilder
        {
            public static AcousticSettingsRaw Build(string s)
            {
                var propertyMap = BuildPropertyMap();

                SystemType systemType = default;
                Rate frameRate = default;
                int sampleCount = default;
                FineDuration sampleStartDelay = default;
                FineDuration samplePeriod = default;
                FineDuration pulseWidth = default;
                PingMode pingMode = default;
                bool enableTransmit = default;
                Frequency frequency = default;
                bool enable150Volts = default;
                float receiverGain = default;
                FocusDistance focusDistance = default;
                FineDuration antiAliasing = default;
                InterpacketDelaySettings interpacketDelay = default;
                Salinity salinity = default;

                foreach (var kvp in SplitPropertyValues(s))
                {
                    var (propertyName, v) = SplitPropertyValue(kvp, propertyMap);
                    StoreValue(propertyName, v);
                }

                if (propertyMap.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"No values provied for {string.Join(", ", propertyMap.Keys)}");
                }

                return new AcousticSettingsRaw(
                    systemType,
                    frameRate,
                    sampleCount,
                    sampleStartDelay,
                    samplePeriod,
                    pulseWidth,
                    pingMode,
                    enableTransmit,
                    frequency,
                    enable150Volts,
                    receiverGain,
                    focusDistance,
                    antiAliasing,
                    interpacketDelay,
                    salinity);

                void StoreValue(string name, object v)
                {
                    switch (name)
                    {
                        case nameof(AcousticSettingsRaw.SystemType):
                            systemType = (SystemType)v;
                            break;
                        case nameof(AcousticSettingsRaw.FrameRate):
                            frameRate = (Rate)v;
                            break;
                        case nameof(AcousticSettingsRaw.SampleCount):
                            sampleCount = (int)v;
                            break;
                        case nameof(AcousticSettingsRaw.SampleStartDelay):
                            sampleStartDelay = (FineDuration)v;
                            break;
                        case nameof(AcousticSettingsRaw.SamplePeriod):
                            samplePeriod = (FineDuration)v;
                            break;
                        case nameof(AcousticSettingsRaw.PulseWidth):
                            pulseWidth = (FineDuration)v;
                            break;
                        case nameof(AcousticSettingsRaw.PingMode):
                            pingMode = (PingMode)v;
                            break;
                        case nameof(AcousticSettingsRaw.EnableTransmit):
                            enableTransmit = (bool)v;
                            break;
                        case nameof(AcousticSettingsRaw.Frequency):
                            frequency = (Frequency)v;
                            break;
                        case nameof(AcousticSettingsRaw.Enable150Volts):
                            enable150Volts = (bool)v;
                            break;
                        case nameof(AcousticSettingsRaw.ReceiverGain):
                            receiverGain = (float)v;
                            break;
                        case nameof(AcousticSettingsRaw.FocusDistance):
                            focusDistance = (FocusDistance)v;
                            break;
                        case nameof(AcousticSettingsRaw.AntiAliasing):
                            antiAliasing = (FineDuration)v;
                            break;
                        case nameof(AcousticSettingsRaw.InterpacketDelay):
                            interpacketDelay = (InterpacketDelaySettings)v;
                            break;
                        case nameof(AcousticSettingsRaw.Salinity):
                            salinity = (Salinity)v;
                            break;

                        default:
                            throw new ArgumentException($"Unhandled propety name [{name}]");
                    }
                }
            }

            private static IEnumerable<string> SplitPropertyValues(string s)
                => s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim());

            private static (string PropertyName, object Value)
                SplitPropertyValue(string s, PropertyMap propertyMap)
            {
                var splits = s.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length != 2)
                {
                    throw new ArgumentException($"Invalid property value: {s}");
                }

                var (propertyName, stringRepresentation) = (splits[0], splits[1]);
                if (stringRepresentation.StartsWith("[", StringComparison.OrdinalIgnoreCase)
                    && stringRepresentation.EndsWith("]", StringComparison.OrdinalIgnoreCase))
                {
                    stringRepresentation = stringRepresentation.Substring(1, stringRepresentation.Length - 2);

                    if (propertyMap.TryGetValue(propertyName, out var build))
                    {
                        propertyMap.Remove(propertyName);

                        var v = build(stringRepresentation);
                        return (propertyName, v);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Non-existent key or duplicate value for [{propertyName}]");
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid property value: {s}");
                }
            }

            private static PropertyMap BuildPropertyMap()
                => new PropertyMap
                {
                    { nameof(AcousticSettingsRaw.SystemType), s => SystemType.GetFromHumanReadableString(s) },
                    { nameof(AcousticSettingsRaw.FrameRate), s => (Rate)double.Parse(s, CultureInfo.InvariantCulture) },
                    { nameof(AcousticSettingsRaw.SampleCount), s => int.Parse(s, CultureInfo.InvariantCulture) },
                    { nameof(AcousticSettingsRaw.SampleStartDelay), ParseFineDuration },
                    { nameof(AcousticSettingsRaw.SamplePeriod), ParseFineDuration },
                    { nameof(AcousticSettingsRaw.PulseWidth), ParseFineDuration },
                    { nameof(AcousticSettingsRaw.PingMode), s => PingMode.GetFrom(int.Parse(s, CultureInfo.InvariantCulture)) },
                    { nameof(AcousticSettingsRaw.EnableTransmit), ParseBool },
                    { nameof(AcousticSettingsRaw.Frequency), s => Enum.Parse(typeof(Frequency), s) },
                    { nameof(AcousticSettingsRaw.Enable150Volts), ParseBool },
                    { nameof(AcousticSettingsRaw.ReceiverGain), s => float.Parse(s, CultureInfo.InvariantCulture) },
                    { nameof(AcousticSettingsRaw.FocusDistance), ParseFocusDistance },
                    { nameof(AcousticSettingsRaw.AntiAliasing), ParseFineDuration },
                    { nameof(AcousticSettingsRaw.InterpacketDelay), ParseInterpacketDelay },
                    { nameof(AcousticSettingsRaw.Salinity), s => Enum.Parse(typeof(Salinity), s) },
                };

            private static object ParseFineDuration(string s) => (FineDuration)double.Parse(s, CultureInfo.InvariantCulture);

            private static object ParseBool(string s) => bool.Parse(s);

            private static object ParseFocusDistance(string s)
            {
                var match = Regex.Match(s, @"(-?\d+[\.?\d+]*)\ m");
                if (match.Groups.Count == 2)
                {
                    var meters = match.Groups[1].Value;
                    var distance = (Distance)double.Parse(meters, CultureInfo.InvariantCulture);
                    return new FocusDistance(distance);
                }
                else
                {
                    var focusUnits = uint.Parse(s, CultureInfo.InvariantCulture);
                    return FocusDistance.FromFocusUnits(focusUnits);
                }
            }

            private static object ParseInterpacketDelay(string s)
            {
                var splits = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var enable = bool.Parse(splits[0]);
                var microseconds = double.Parse(splits[1], CultureInfo.InvariantCulture);
                return new InterpacketDelaySettings { Enable = enable, Delay = (FineDuration)microseconds };
            }
        }
    }
}
