// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using static SoundMetrics.Aris.Core.MathSupport;

namespace SoundMetrics.Aris.Core
{
#pragma warning disable CA2225 // Operator overloads have named alternates

    /// <summary>
    /// Represents a fine-grained time span or duration.
    /// The &quot;default&quot; unit used in construction of or during
    /// computation within is microseconds.
    /// </summary>
    [DebuggerDisplay("{TotalSeconds}s")]
    [DataContract]
    [JsonConverter(typeof(FineDurationJsonConverter))]
    public struct FineDuration : IComparable<FineDuration>, IEquatable<FineDuration>
    {
        public static readonly FineDuration Zero = FineDuration.FromMicroseconds(0);
        public static readonly FineDuration OneMicrosecond = FineDuration.FromMicroseconds(1);

        [DataMember]
        private readonly double _microseconds;

        private FineDuration(long microseconds)
        {
            _microseconds = microseconds;
        }

        private FineDuration(double microseconds)
        {
            _microseconds = microseconds;
        }

        public static explicit operator FineDuration(double microseconds) => FineDuration.FromMicroseconds(microseconds);

        public double TotalMicroseconds { get { return _microseconds; } }
        public double TotalMilliseconds { get { return _microseconds / 1000.0; } }
        public double TotalSeconds { get { return _microseconds / (1000.0 * 1000.0); } }

        public FineDuration Abs() => new FineDuration(Math.Abs(_microseconds));

        public TimeSpan ToTimeSpan() => TimeSpan.FromMilliseconds(TotalMilliseconds);

        public static FineDuration FromNanosecond(long nanoseconds)
        {
            return new FineDuration((double)nanoseconds / 1000.0);
        }

        private static FineDuration FromMicroseconds(double microseconds)
        {
            return new FineDuration(microseconds);
        }

        public static FineDuration FromMilliseconds(double milliseconds)
        {
            return new FineDuration(milliseconds * 1000.0);
        }

        public static FineDuration FromSeconds(double seconds)
        {
            return new FineDuration(seconds * (1000 * 1000));
        }

        public FineDuration RoundToMicroseconds()
            => FineDuration.FromMicroseconds(RoundAway(this.TotalMicroseconds));

        /// <summary>
        /// Performs a Floor() on FineDuration's native unit, which is microseconds.
        /// </summary>
        public FineDuration Floor => FineDuration.FromMicroseconds(Math.Floor(TotalMicroseconds));

        /// <summary>
        /// Performs a Ceiling() on FineDuration's native unit, which is microseconds.
        /// </summary>
        public FineDuration Ceiling => FineDuration.FromMicroseconds(Math.Ceiling(TotalMicroseconds));

        public override bool Equals(object obj)
            => (obj is FineDuration) ? Equals((FineDuration)obj) : false;

        public bool Equals(FineDuration other) => this._microseconds == other._microseconds;

        public override int GetHashCode() => _microseconds.GetHashCode();

        public static FineDuration operator /(FineDuration a, double b)
        {
            return new FineDuration((long)(a._microseconds / b));
        }

        public static double operator /(FineDuration a, Rate b)
        {
            return (a._microseconds / b.Hz);
        }

        public static FineDuration operator +(FineDuration a)
        {
            return a;
        }

        public static FineDuration operator -(FineDuration a)
        {
            return new FineDuration(-a._microseconds);
        }

        public static bool operator ==(FineDuration a, FineDuration b)
        {
            return a._microseconds == b._microseconds;
        }

        public static bool operator !=(FineDuration a, FineDuration b)
        {
            return !(a == b);
        }

        public static bool operator <(FineDuration a, FineDuration b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator <=(FineDuration a, FineDuration b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >(FineDuration a, FineDuration b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator >=(FineDuration a, FineDuration b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static FineDuration operator +(FineDuration a, FineDuration b)
        {
            return FineDuration.FromMicroseconds(a.TotalMicroseconds + b.TotalMicroseconds);
        }

        public static FineDuration operator -(FineDuration a, FineDuration b)
        {
            return FineDuration.FromMicroseconds(a.TotalMicroseconds - b.TotalMicroseconds);
        }

        public static FineDuration operator *(FineDuration duration, double multiplier)
        {
            return FineDuration.FromMicroseconds((long)(duration.TotalMicroseconds * multiplier));
        }

        public static FineDuration operator *(double multiplier, FineDuration duration)
        {
            return FineDuration.FromMicroseconds((long)(duration.TotalMicroseconds * multiplier));
        }

        public static double operator /(FineDuration a, FineDuration b)
        {
            return (double)a._microseconds / b._microseconds;
        }

        public static Rate operator /(double count, FineDuration duration)
        {
            return new Rate(count, duration);
        }

        public override string ToString()
            => string.Format(CultureInfo.CurrentCulture, "{0:0.000000} s", this.TotalSeconds);

        public static FineDuration Min(FineDuration a, FineDuration b)
        {
            return a._microseconds < b._microseconds ? a : b;
        }

        public static FineDuration Max(FineDuration a, FineDuration b)
        {
            return a._microseconds > b._microseconds ? a : b;
        }

        public int CompareTo(FineDuration other)
        {
            double difference = this._microseconds - other._microseconds;
            return difference < 0 ? -1 : (difference == 0 ? 0 : +1);
        }
    }

#pragma warning restore CA2225 // Operator overloads have named alternates
}
