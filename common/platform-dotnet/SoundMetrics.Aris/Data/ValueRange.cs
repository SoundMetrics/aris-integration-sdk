using System;

namespace SoundMetrics.Aris.Data
{
    public struct ValueRange<T>
        where T : struct, IComparable<T>
    {
        public ValueRange(T min, T max)
        {
            Min = min;
            Max = max;
        }

        public T Min;
        public T Max;

        public bool IsReverseRange => Min.CompareTo(Max) > 0;

        public override string ToString()
        {
            return $"{Min}-{Max}";
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

            return @this.Min.CompareTo(value) <= 0 && value.CompareTo(@this.Max) <= 0;
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
                throw new ArgumentException($"{nameof(min)} may not be greater than {nameof(max)}");
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
