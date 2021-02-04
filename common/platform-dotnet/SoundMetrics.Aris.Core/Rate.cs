// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{RatePerSecond}/s")]
    public struct Rate : IComparable<Rate>
    {
        private readonly double _count;
        private readonly FineDuration _duration;

        internal Rate(double count, FineDuration duration)
        {
            _count = count;
            _duration = duration;
        }

        public static explicit operator Rate(double count) => Rate.PerSecond(count);

        public double RatePerSecond
        {
            get { return _count / _duration.TotalSeconds; }
        }

        public FineDuration Period => _duration / _count;

        public static Rate PerSecond(double count)
            => new Rate(count, FineDuration.FromSeconds(1.0));

        public static Rate Hertz(double count) => PerSecond(count);

        public static Rate PerMillisecond(double count)
        {
            return new Rate(count, FineDuration.FromMilliseconds(1.0));
        }

        public static readonly Rate Zero = Hertz(0);

        public static readonly Rate OneHertz = Hertz(1);

        public static FineDuration operator /(double count, Rate rate)
        {
            return (count / rate._count) * rate._duration;
        }

        public static bool operator <(Rate a, Rate b) => a.RatePerSecond < b.RatePerSecond;
        public static bool operator <=(Rate a, Rate b) => a.RatePerSecond <= b.RatePerSecond;
        public static bool operator >(Rate a, Rate b) => a.RatePerSecond > b.RatePerSecond;
        public static bool operator >=(Rate a, Rate b) => a.RatePerSecond >= b.RatePerSecond;
        public static bool operator ==(Rate a, Rate b) => a.RatePerSecond == b.RatePerSecond;
        public static bool operator !=(Rate a, Rate b) => !(a.RatePerSecond == b.RatePerSecond);

        public static Rate Min(Rate a, Rate b) => a < b ? a : b;
        public static Rate Max(Rate a, Rate b) => a > b ? a : b;

        public override bool Equals(object obj)
        {
            if (obj is Rate other)
            {
                return this == other;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return RatePerSecond.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}/s", this.RatePerSecond);
        }

        public int CompareTo(Rate other)
            => RatePerSecond.CompareTo(other.RatePerSecond);
    }
}
