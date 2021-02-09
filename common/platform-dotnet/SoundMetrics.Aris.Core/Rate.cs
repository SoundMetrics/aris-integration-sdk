// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{Hz}/s"), TypeConverter(typeof(Converters.RateConverter))]
    public struct Rate : IComparable<Rate>, IEquatable<Rate>
    {
        private readonly double _count;
        private readonly FineDuration _duration;

        internal Rate(double count, FineDuration duration)
        {
            _count = count;
            _duration = duration;
        }

        public static explicit operator Rate(double count) => Rate.FromHertz(count);

        public double Hz => _count / _duration.TotalSeconds;

        public double KHz => Hz / 1_000;

        public double MHz => Hz / 1_000_000;

        public FineDuration Period => _duration / _count;

        public static Rate FromHertz(double count)
            => new Rate(count, FineDuration.FromSeconds(1.0));

        public static Rate PerMillisecond(double count)
        {
            return new Rate(count, FineDuration.FromMilliseconds(1.0));
        }

        public static readonly Rate Zero = FromHertz(0);

        public static readonly Rate OneHertz = FromHertz(1);

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
            => string.Format("{0}/s", this.Hz);

        public int CompareTo(Rate other)
            => Hz.CompareTo(other.Hz);
    }
}
