// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;

namespace SoundMetrics.Aris.Core
{
#pragma warning disable CA2225 // Operator overloads have named alternates

    [DebuggerDisplay("{Hz}/s"), TypeConverter(typeof(Converters.RateConverter))]
    [DataContract]
    public struct Rate : IComparable<Rate>, IEquatable<Rate>
    {
        [DataMember]
        private readonly double _count;

        [DataMember]
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

        public override bool Equals(object obj)
            => (obj is Rate) ? Equals((Rate)obj) : false;

        public bool Equals(Rate other) => this.Hz == other.Hz;

        public override int GetHashCode() => Hz.GetHashCode();

        public override string ToString()
            => string.Format(CultureInfo.CurrentCulture, "{0}/s", this.Hz);

        public int CompareTo(Rate other)
            => Hz.CompareTo(other.Hz);
    }

#pragma warning restore CA2225 // Operator overloads have named alternates
}
