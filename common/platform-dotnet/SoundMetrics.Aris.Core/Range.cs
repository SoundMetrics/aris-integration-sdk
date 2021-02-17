// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    public struct Range<T> : IEquatable<Range<T>> where T : IComparable, IComparable<T>
    {
        public Range(T minimum, T maximum)
        {
            if (maximum.CompareTo(minimum) < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximum),
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    "Maximum must not be less than minimum");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
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

        public override bool Equals(object obj)
            => obj is Range<T> other && this.Equals(other);

        public bool Equals(Range<T> other)
            => this.minimum.Equals(other.minimum)
                && this.maximum.Equals(other.maximum);

        public static bool operator ==(Range<T> a, Range<T> b)
            => a.Equals(b);

        public static bool operator !=(Range<T> a, Range<T> b)
            => !a.Equals(b);

        public override int GetHashCode()
            => minimum.GetHashCode() ^ maximum.GetHashCode();

        private readonly T minimum;
        private readonly T maximum;
    }
}
