// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    public struct Range<T> where T : IComparable, IComparable<T>
    {
        public Range(T minimum, T maximum)
        {
            if (maximum.CompareTo(minimum) < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximum), "Maximum must not be less than minimum");
            }

            this.minimum = minimum;
            this.maximum = maximum;
        }

        public T Minimum => minimum;
        public T Maximum => maximum;

        public bool ContainsValue(T value)
        {
            return
                minimum.CompareTo(value) <= 0 && value.CompareTo(maximum) <= 0;
        }

        public bool ContainsRange(Range<T> other)
        {
            return ContainsValue(other.minimum) && ContainsValue(other.maximum);
        }

        public Range<T> ConstrainTo(Range<T> other)
        {
            var newMin = minimum.CompareTo(other.minimum) >= 0 ? minimum : other.minimum;
            var newMax = maximum.CompareTo(other.maximum) <= 0 ? maximum : other.maximum;
            newMax = newMax.CompareTo(newMin) >= 0 ? newMax : newMin;

            return new Range<T>(newMin, newMax);
        }

        public Range<T> ConstrainMaxTo(T value)
        {
            var newMax = maximum.CompareTo(value) <= 0 ? maximum : value;
            return new Range<T>(minimum, newMax);
        }

        public override string ToString() => $"[{Minimum}-{Maximum}]";

        private readonly T minimum;
        private readonly T maximum;
    }
}
