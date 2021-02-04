// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{TotalSeconds}s")]
    public struct FineDuration : IComparable<FineDuration>, IEquatable<FineDuration>
    {
        public static readonly FineDuration Zero = FineDuration.FromMicroseconds(0);
        public static readonly FineDuration OneMicrosecond = FineDuration.FromMicroseconds(1);

        private readonly double _microseconds;

        private FineDuration(long microseconds)
        {
            _microseconds = microseconds;
        }

        private FineDuration(double microseconds)
        {
            _microseconds = microseconds;
        }

        public double TotalMicroseconds { get { return _microseconds; } }
        public double TotalMilliseconds { get { return _microseconds / 1000.0; } }
        public double TotalSeconds { get { return _microseconds / (1000.0 * 1000.0); } }

        public TimeSpan ToTimeSpan() => TimeSpan.FromMilliseconds(TotalMilliseconds);

        public static FineDuration FromNanosecond(long nanoseconds)
        {
            return new FineDuration((double)nanoseconds / 1000.0);
        }

        public static FineDuration FromMicroseconds(double microseconds)
        {
            return new FineDuration(microseconds);
        }

        public static FineDuration FromMilliseconds(double milliseconds)
        {
            return new FineDuration((long)(milliseconds * 1000.0));
        }

        public static FineDuration FromSeconds(double seconds)
        {
            return new FineDuration((long)(seconds * (1000 * 1000.0)));
        }

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
            return (a._microseconds / b.RatePerSecond);
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
        {
            return string.Format("{0:0.000000} s", this.TotalSeconds);
        }

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

    //public struct FineDuration
    //    : IComparable, IComparable<FineDuration>, IEquatable<FineDuration>
    //{
    //    public static FineDuration Zero = FromMicroseconds(0.0);
    //    public static FineDuration OneMicrosecond = FromMicroseconds(1.0);

    //    public static FineDuration FromMicroseconds(double microseconds) =>
    //        new FineDuration(microseconds);

    //    public static FineDuration FromMilliseconds(double milliseconds) =>
    //        new FineDuration(milliseconds * 1_000.0);

    //    public double TotalMicroseconds => microseconds;
    //    public double TotalMilliseconds => microseconds / 1_000.0;
    //    public double TotalSeconds => microseconds / 1_000_000.0;

    //    public static FineDuration operator +(FineDuration d) => d;
    //    public static FineDuration operator -(FineDuration d) => FromMicroseconds(-d.microseconds);

    //    public static FineDuration operator +(FineDuration d1, FineDuration d2) =>
    //        FromMicroseconds(d1.TotalMicroseconds + d2.TotalMicroseconds);
    //    public static FineDuration operator -(FineDuration d1, FineDuration d2) =>
    //        FromMicroseconds(d1.TotalMicroseconds - d2.TotalMicroseconds);

    //    public static FineDuration operator *(FineDuration d, double multiplier)
    //        => FromMicroseconds(d.TotalMicroseconds * multiplier);
    //    public static FineDuration operator *(double multiplier, FineDuration d)
    //        => FromMicroseconds(d.TotalMicroseconds * multiplier);

    //    public static FineDuration operator /(FineDuration d, double divisor)
    //    {
    //        if (divisor == 0.0)
    //        {
    //            throw new DivideByZeroException();
    //        }

    //        return FromMicroseconds(d.TotalMicroseconds / divisor);
    //    }

    //    public static FineDuration operator /(double f, FineDuration divisor)
    //    {
    //        if (divisor == Zero)
    //        {
    //            throw new DivideByZeroException();
    //        }

    //        return FromMicroseconds(f / divisor.TotalMicroseconds);
    //    }

    //    public static bool operator ==(FineDuration a, FineDuration b) =>
    //        a.TotalMicroseconds == b.TotalMicroseconds;
    //    public static bool operator !=(FineDuration a, FineDuration b) =>
    //        a.TotalMicroseconds != b.TotalMicroseconds;

    //    public static bool operator <(FineDuration a, FineDuration b) =>
    //        a.microseconds < b.microseconds;
    //    public static bool operator <=(FineDuration a, FineDuration b) =>
    //        a.microseconds <= b.microseconds;
    //    public static bool operator >=(FineDuration a, FineDuration b) =>
    //        a.microseconds >= b.microseconds;
    //    public static bool operator >(FineDuration a, FineDuration b) =>
    //        a.microseconds > b.microseconds;

    //    public override string ToString() =>
    //        $"{microseconds:0.000} \u00b5s";

    //    public bool Equals(FineDuration other)
    //        => this.microseconds == other.microseconds;

    //    public int CompareTo(object obj)
    //    {
    //        if (obj is FineDuration other)
    //        {
    //            return ((IComparable<double>)microseconds).CompareTo(other.microseconds);
    //        }
    //        else
    //        {
    //            return obj.Equals(this) ? 0 : 1;
    //        }
    //    }

    //    public int CompareTo(FineDuration other) =>
    //        ((IComparable<double>)microseconds).CompareTo(other.microseconds);

    //    public override bool Equals(object obj)
    //    {
    //        return obj is FineDuration duration &&
    //               microseconds == duration.microseconds;
    //    }

    //    public override int GetHashCode()
    //    {
    //        return microseconds.GetHashCode();
    //    }

    //    private FineDuration(double microseconds)
    //    {
    //        this.microseconds = microseconds;
    //    }

    //    private readonly double microseconds;
    //}
}
