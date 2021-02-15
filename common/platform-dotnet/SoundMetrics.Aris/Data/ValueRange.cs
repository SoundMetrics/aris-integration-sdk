using System;

namespace SoundMetrics.Aris.Data
{
    public struct ValueRange<T> : IEquatable<ValueRange<T>>
        where T : struct, IComparable<T>
    {
        public ValueRange(T min, T max)
        {
            Min = min;
            Max = max;
        }

#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly T Min;
        public readonly T Max;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public bool IsReverseRange => Min.CompareTo(Max) > 0;

        public override string ToString()
        {
            return $"{Min}-{Max}";
        }

        public override bool Equals(object? obj)
            => obj is ValueRange<T> other && this.Equals(other);

        public bool Equals(ValueRange<T> other)
            => this.Min.Equals(other.Min) && this.Max.Equals(other.Max);

        public override int GetHashCode()
            => Min.GetHashCode() ^ Max.GetHashCode();

        public static bool operator ==(ValueRange<T> left, ValueRange<T> right)
            => left.Equals(right);

        public static bool operator !=(ValueRange<T> left, ValueRange<T> right)
            => !(left == right);
    }

    public static class RangeExtensions
    {
        public static bool Contains<T>(this ValueRange<T> me, T value)
            where T : struct, IComparable<T>
        {
            if (me.IsReverseRange)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new InvalidOperationException("Negative range is not allowed.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            return me.Min.CompareTo(value) <= 0 && value.CompareTo(me.Max) <= 0;
        }

        public static bool IsSubrangeOf<T>(this ValueRange<T> @this, ValueRange<T> other)
            where T : struct, IComparable<T>
        {
            return other.Contains(@this.Min) && other.Contains(@this.Max);
        }

        public static ValueRange<T> Constrain<T>(
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

            T newMin = Greater(@this.Min, min);
            T newMax = Lesser(@this.Max, max);

            return new ValueRange<T>(newMin, newMax);
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
