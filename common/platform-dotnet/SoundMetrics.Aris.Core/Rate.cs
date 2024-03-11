// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;

namespace SoundMetrics.Aris.Core
{
#pragma warning disable CA2225 // Operator overloads have named alternates

    [DebuggerDisplay("{Hz}/s"), TypeConverter(typeof(Converters.RateConverter))]
    [JsonConverter(typeof(RateJsonConverter))]
    public struct Rate : IComparable<Rate>, IEquatable<Rate>
    {
        private readonly double _count;

        private readonly FineDuration _duration;

        internal Rate(double count, FineDuration duration)
        {
            _count = count;
            _duration = duration;
        }

        public static explicit operator Rate(double count) => Rate.ToRate(count);

        public static Rate ToRate(double countsPerSecond)
            => new Rate(countsPerSecond, FineDuration.FromSeconds(1.0));

        public double Hz => _count / _duration.TotalSeconds;

        public double KHz => Hz / 1_000;

        public double MHz => Hz / 1_000_000;

        public FineDuration Period => _duration / _count;

        public Rate NormalizeToHertz()
            => Rate.ToRate(_count * (1 / _duration).Hz);

        public Rate Abs() => new Rate(_count, _duration.Abs());


        public static Rate PerMillisecond(double count)
        {
            return new Rate(count, FineDuration.FromMilliseconds(1.0));
        }

        public static readonly Rate Zero = ToRate(0);

        public static readonly Rate OneHertz = ToRate(1);

        public static Rate operator +(Rate a, Rate b) => (Rate)(a.Hz + b.Hz);
        public static Rate operator -(Rate a, Rate b) => (Rate)(a.Hz - b.Hz);

        public static Rate operator *(Rate rate, double multiplier)
            => (Rate)(rate.Hz * multiplier);

        public static Rate operator *(double multiplier, Rate rate)
            => (Rate)(rate.Hz * multiplier);

        public static double operator *(Rate rate, TimeSpan timespan)
            => timespan.TotalMicroseconds * rate._count / rate._duration.TotalMicroseconds;

        public static double operator *(TimeSpan timespan, Rate rate)
            => rate * timespan;

        public static Rate operator /(Rate rate, double divisor)
            => rate * (1 / divisor);

        public static FineDuration operator /(double count, Rate rate)
        {
            return (count / rate._count) * rate._duration;
        }

        public static bool operator <(Rate a, Rate b) => a.Hz < b.Hz;
        public static bool operator <=(Rate a, Rate b) => a.Hz <= b.Hz;
        public static bool operator >(Rate a, Rate b) => a.Hz > b.Hz;
        public static bool operator >=(Rate a, Rate b) => a.Hz >= b.Hz;
        public static bool operator ==(Rate a, Rate b) => a.Hz == b.Hz;
        public static bool operator !=(Rate a, Rate b) => !(a.Hz == b.Hz);

        public static Rate Min(Rate a, Rate b) => a < b ? a : b;
        public static Rate Max(Rate a, Rate b) => a > b ? a : b;

        public override bool Equals(object? obj)
            => (obj is Rate) ? Equals((Rate)obj) : false;

        public bool Equals(Rate other) => this.Hz == other.Hz;

        public override int GetHashCode() => Hz.GetHashCode();

        public override string ToString()
            => string.Format(CultureInfo.CurrentCulture, "{0}/s", this.Hz);

        internal string ToSerializationString()
            => string.Format(CultureInfo.InvariantCulture, "{0}/{1}", _count, _duration.TotalMicroseconds);

        internal static bool TryParseSerializationString(string? s, out Rate result)
        {
            if (s is null)
            {
                result = default;
                return false;
            }
            else
            {
                // After upgrading past .NET Standard 2.0, convert input to ReadOnlySpan<char>.
                var splits = s.Split(SerializationSeparatorList, 3, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length == 2
                    && double.TryParse(splits[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var count)
                    && double.TryParse(splits[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var duration))
                {
                    result = new Rate(count, (FineDuration)duration);
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }
        }

        private static readonly string[] SerializationSeparatorList = new[] { "/" };

        public int CompareTo(Rate other)
            => Hz.CompareTo(other.Hz);
    }

#pragma warning restore CA2225 // Operator overloads have named alternates
}
