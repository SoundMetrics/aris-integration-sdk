// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    public struct ValueRange<T> : IEquatable<ValueRange<T>>
        where T : struct, IComparable<T>
    {
        public ValueRange(T min, T max)
        {
            Minimum = min;
            Maximum = max;
        }

#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly T Minimum;
        public readonly T Maximum;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public bool IsEmpty => Minimum.Equals(Maximum) || Minimum.CompareTo(Maximum) > 0;

        public bool IsReverseRange => Minimum.CompareTo(Maximum) > 0;

        public override string ToString()
        {
            return $"{Minimum}-{Maximum}";
        }

        public override bool Equals(object obj)
            => obj is ValueRange<T> other && this.Equals(other);

        public bool Equals(ValueRange<T> other)
            => this.Minimum.Equals(other.Minimum)
                && this.Maximum.Equals(other.Maximum);

        public override int GetHashCode()
            => Minimum.GetHashCode() ^ Maximum.GetHashCode();

        public static bool operator ==(ValueRange<T> left, ValueRange<T> right)
            => left.Equals(right);

        public static bool operator !=(ValueRange<T> left, ValueRange<T> right)
            => !(left == right);
    }

    public static class RangeExtensions
    {
        public static bool Contains<T>(this ValueRange<T> @this, T value)
            where T : struct, IComparable<T>
        {
            if (@this.IsReverseRange)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new InvalidOperationException("Negative range is not allowed.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            return @this.Minimum.CompareTo(value) <= 0 && value.CompareTo(@this.Maximum) <= 0;
        }

        public static bool Contains<T>(this ValueRange<T> @this, ValueRange<T> other)
            where T : struct, IComparable<T>
        {
            return @this.Contains(other.Minimum) && @this.Contains(other.Maximum);
        }

        private static ValueRange<T> ConstrainTo<T>(
            this ValueRange<T> @this,
            T? min,
            T? max)
            where T : struct, IComparable<T>
        {
            if (min.HasValue && max.HasValue && min.Value.CompareTo(max.Value) > 0)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException($"{nameof(min)} may not be greater than {nameof(max)}");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            T newMin = Greater(@this.Minimum, min);
            T newMax = Lesser(@this.Maximum, max);

            return new ValueRange<T>(newMin, newMax);
        }

        public static ValueRange<T> ConstrainTo<T>(this ValueRange<T> @this, in ValueRange<T> that)
            where T : struct, IComparable<T>
            =>
                @this.ConstrainTo(that.Minimum, that.Maximum);

        public static T ConstrainTo<T>(this T t, in ValueRange<T> range)
            where T : struct, IComparable<T>
            => Greater(
                range.Minimum,
                Lesser(range.Maximum, t));

        public static ValueRange<T> Intersect<T>(
            this ValueRange<T> @this,
            in ValueRange<T> that)
            where T : struct, IComparable<T>
        {
            throw new NotImplementedException();
        }

        public static ValueRange<T> Union<T>(
            this ValueRange<T> @this,
            in ValueRange<T> that)
            where T : struct, IComparable<T>
        {
            if (@this.Intersect(that).IsEmpty)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Cannot represent a sparse range");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            throw new NotImplementedException();
        }

        internal static T Lesser<T>(in T t, in T? other)
            where T : struct, IComparable<T>
            =>
                other.HasValue ? Lesser(t, other.Value) : t;

        internal static T Lesser<T>(in T t, in T other)
            where T : struct, IComparable<T>
            =>
                t.CompareTo(other) < 0 ? t : other;

        internal static T Greater<T>(in T t, in T? other)
            where T : struct, IComparable<T>
            =>
                other.HasValue ? Greater(t, other.Value) : t;

        internal static T Greater<T>(in T t, in T other)
            where T : struct, IComparable<T>
            =>
                t.CompareTo(other) > 0 ? t : other;
    }
}
