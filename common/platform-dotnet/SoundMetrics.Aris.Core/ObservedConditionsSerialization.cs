using SoundMetrics.Aris.Core.Raw;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SoundMetrics.Aris.Core
{
    using PropertyMap = Dictionary<string, Func<string, object>>;

    /// <summary>
    /// Serialization methods used for parsing log files and
    /// constructing unit tests.
    /// </summary>
    public static class ObservedConditionsSerialization
    {
        public static string Serialize(ObservedConditions observedConditions)
        {
            if (observedConditions is null)
            {
                throw new ArgumentNullException(nameof(observedConditions));
            }

            return
                  $"{nameof(ObservedConditions.WaterTemp)}=[{observedConditions.WaterTemp.DegreesCelsius}]; "
                + $"{nameof(ObservedConditions.Depth)}=[{observedConditions.Depth.Meters}]";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters",
            Justification = "Policy")]
        public static ObservedConditions Deserialize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentException($"'{nameof(s)}' cannot be null or whitespace.", nameof(s));
            }


            var conditoins = Builder.Build(s);
            return conditoins;
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

        private static class Builder
        {
            public static ObservedConditions Build(string s)
            {
                var propertyMap = BuildPropertyMap();

                Temperature waterTemp = default;
                Distance depth = default;

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

                return new ObservedConditions(waterTemp, depth);

                void StoreValue(string name, object v)
                {
                    switch (name)
                    {
                        case nameof(ObservedConditions.WaterTemp):
                            waterTemp = (Temperature)v;
                            break;
                        case nameof(ObservedConditions.Depth):
                            depth = (Distance)v;
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
                    { nameof(ObservedConditions.WaterTemp), s => (Temperature)double.Parse(s, CultureInfo.InvariantCulture) },
                    { nameof(ObservedConditions.Depth), s => (Distance)double.Parse(s, CultureInfo.InvariantCulture) },
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
