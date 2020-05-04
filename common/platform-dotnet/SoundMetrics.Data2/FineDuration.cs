// Copyright 2011-2020 Sound Metrics Corp. All Rights Reserved.

using System;

namespace SoundMetrics.Data
{
    public struct FineDuration
        : IComparable, IComparable<FineDuration>, IEquatable<FineDuration>
    {
        public static FineDuration Zero = FromMicroseconds(0.0);
        public static FineDuration OneMicrosecond = FromMicroseconds(1.0);

        public static FineDuration FromMicroseconds(double microseconds) =>
            new FineDuration(microseconds);

        public static FineDuration FromMilliseconds(double milliseconds) =>
            new FineDuration(milliseconds * 1_000.0);

        public double TotalMicroseconds => microseconds;
        public double TotalMilliseconds => microseconds / 1_000.0;
        public double TotalSeconds => microseconds / 1_000_000.0;

        public static FineDuration operator +(FineDuration d) => d;
        public static FineDuration operator -(FineDuration d) => FromMicroseconds(-d.microseconds);

        public static FineDuration operator +(FineDuration d1, FineDuration d2) =>
            FromMicroseconds(d1.TotalMicroseconds + d2.TotalMicroseconds);
        public static FineDuration operator -(FineDuration d1, FineDuration d2) =>
            FromMicroseconds(d1.TotalMicroseconds - d2.TotalMicroseconds);

        public static FineDuration operator *(FineDuration d, double multiplier)
            => FromMicroseconds(d.TotalMicroseconds * multiplier);
        public static FineDuration operator *(double multiplier, FineDuration d)
            => FromMicroseconds(d.TotalMicroseconds * multiplier);

        public static FineDuration operator /(FineDuration d, double divisor)
        {
            if (divisor == 0.0)
            {
                throw new DivideByZeroException();
            }

            return FromMicroseconds(d.TotalMicroseconds / divisor);
        }

        public static FineDuration operator /(double f, FineDuration divisor)
        {
            if (divisor == Zero)
            {
                throw new DivideByZeroException();
            }

            return FromMicroseconds(f / divisor.TotalMicroseconds);
        }

        public static bool operator ==(FineDuration a, FineDuration b) =>
            a.TotalMicroseconds == b.TotalMicroseconds;
        public static bool operator !=(FineDuration a, FineDuration b) =>
            a.TotalMicroseconds != b.TotalMicroseconds;

        public static bool operator <(FineDuration a, FineDuration b) =>
            a.microseconds < b.microseconds;
        public static bool operator <=(FineDuration a, FineDuration b) =>
            a.microseconds <= b.microseconds;
        public static bool operator >=(FineDuration a, FineDuration b) =>
            a.microseconds >= b.microseconds;
        public static bool operator >(FineDuration a, FineDuration b) =>
            a.microseconds > b.microseconds;

        public override string ToString() =>
            $"{microseconds:0.000} \u00b5s";

        public bool Equals(FineDuration other)
            => this.microseconds == other.microseconds;

        public int CompareTo(object obj)
        {
            if (obj is FineDuration other)
            {
                return ((IComparable<double>)microseconds).CompareTo(other.microseconds);
            }
            else
            {
                return obj.Equals(this) ? 0 : 1;
            }
        }

        public int CompareTo(FineDuration other) =>
            ((IComparable<double>)microseconds).CompareTo(other.microseconds);

        public override bool Equals(object obj)
        {
            return obj is FineDuration duration &&
                   microseconds == duration.microseconds;
        }

        public override int GetHashCode()
        {
            return microseconds.GetHashCode();
        }

        private FineDuration(double microseconds)
        {
            this.microseconds = microseconds;
        }

        private readonly double microseconds;
    }
}
