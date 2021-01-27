// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    public struct ValueRange<T>
        where T : struct, IComparable<T>
    {
        public ValueRange(T min, T max)
        {
            Minimum = min;
            Maximum = max;
        }

        public T Minimum;
        public T Maximum;

        public bool IsEmpty => Minimum.Equals(Maximum) || Minimum.CompareTo(Maximum) > 0;

        public bool IsReverseRange => Minimum.CompareTo(Maximum) > 0;

        public override string ToString()
        {
            return $"{Minimum}-{Maximum}";
        }
    }

    public static class RangeExtensions
    {
        public static bool Contains<T>(this ValueRange<T> @this, T value)
            where T : struct, IComparable<T>
        {
            if (@this.IsReverseRange)
            {
                throw new InvalidOperationException("Negative range is not allowed.");
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
                throw new ArgumentException($"{nameof(min)} may not be greater than {nameof(max)}");
            }

            T newMin = Greater(@this.Minimum, min);
            T newMax = Lesser(@this.Maximum, max);

            return new ValueRange<T>(newMin, newMax);
        }

        public static ValueRange<T> ConstrainTo<T>(this ValueRange<T> @this, in ValueRange<T> that)
            where T : struct, IComparable<T>
            =>
                @this.ConstrainTo(that.Minimum, that.Maximum);

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
                throw new ArgumentException("Cannot represent a sparse range");
            }

            throw new NotImplementedException();
        }

        internal static T Lesser<T>(in T t, T? other)
            where T : struct, IComparable<T>
        {
            return
                other.HasValue
                    ? (t.CompareTo(other.Value) < 0 ? t : other.Value)
                    : t;
        }

        internal static T Greater<T>(in T t, T? other)
            where T : struct, IComparable<T>
        {
            return
                other.HasValue
                    ? (t.CompareTo(other.Value) > 0 ? t : other.Value)
                    : t;
        }
    }
}
