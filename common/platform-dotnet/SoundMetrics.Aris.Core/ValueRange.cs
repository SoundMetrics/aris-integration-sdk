﻿// Copyright (c) 2010-2024 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    /// <summary>
    /// Describes a range [a,b) of `T`.
    /// </summary>
    [DebuggerDisplay("[{Minimum}, {Maximum}]")]
    public struct ValueRange<T> : IEquatable<ValueRange<T>>
        where T : struct, IComparable
    {
        public ValueRange(T min, T max)
        {
            Minimum = min;
            Maximum = max;
        }

        public ValueRange(in (T min, T max) range)
            : this(range.min, range.max)
        {
        }

        public T Minimum { get; private set; }
        public T Maximum { get; private set; }

        public bool IsEmpty => Minimum.Equals(Maximum) || Minimum.CompareTo(Maximum) > 0;

        public bool IsReverseRange => Minimum.CompareTo(Maximum) > 0;

        public void Deconstruct(out T minimum, out T maximum)
        {
            minimum = Minimum;
            maximum = Maximum;
        }

        public override string ToString()
        {
            return $"[{Minimum}, {Maximum}]";
        }

        public override bool Equals(object? obj)
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
            where T : struct, IComparable
        {
            if (@this.IsReverseRange)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new InvalidOperationException("Negative range is not allowed.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            return @this.Minimum.CompareTo(value) <= 0 && value.CompareTo(@this.Maximum) < 0;
        }

        public static bool Contains<T>(this ValueRange<T> @this, ValueRange<T> other)
            where T : struct, IComparable
        {
            return @this.Contains(other.Minimum) && @this.Contains(other.Maximum);
        }

        public static bool Intersects<T>(
            this in ValueRange<T> a,
            in ValueRange<T> b)
            where T : struct, IComparable
        {
            return !a.Intersect(b).IsEmpty;
        }

        public static ValueRange<T> Intersect<T>(
            this in ValueRange<T> a,
            in ValueRange<T> b)
            where T : struct, IComparable
        {
            var aIsLess = a.Minimum.CompareTo(b.Minimum) <= 0;
            var left = aIsLess ? a : b;
            var right = aIsLess ? b : a;

            if (right.Minimum.CompareTo(left.Maximum) >= 0)
            {
                return new ValueRange<T>(left.Maximum, left.Maximum);
            }
            else
            {
                var lesserMax = Lesser(left.Maximum, right.Maximum);
                return new ValueRange<T>(right.Minimum, lesserMax);
            }
        }

        public static ValueRange<T> Union<T>(
            this ValueRange<T> @this,
            in ValueRange<T> that)
            where T : struct, IComparable
        {
            if (@this.IsEmpty) return that;
            if (that.IsEmpty) return @this;

            Debug.Assert(!@this.IsEmpty);
            Debug.Assert(!that.IsEmpty);

            if (@this.Intersect(that).IsEmpty && !@this.IsAdjacent(that))
            {
                throw new InvalidOperationException(
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    $"Cannot represent a sparse range, given {@this} & {that}");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var leastMin = Lesser(@this.Minimum, that.Minimum);
            var greatestMax = Greater(@this.Maximum, that.Maximum);
            return new ValueRange<T>(leastMin, greatestMax);
        }

        internal static bool IsAdjacent<T>(this in ValueRange<T> a, in ValueRange<T> b)
            where T : struct, IComparable
        {
            var (left, right) = Order(a, b);
            return left.Maximum.Equals(right.Minimum);
        }

        /// <summary>
        /// Returns a tuple of `a` and `b` in ascending order
        /// based on the value of `Minimum`.
        /// </summary>
        internal static (ValueRange<T>, ValueRange<T>) Order<T>(
            in ValueRange<T> a,
            in ValueRange<T> b)
            where T : struct, IComparable
            => (a.Minimum.CompareTo(b.Minimum) <= 0) ? (a, b) : (b, a);

        internal static T Lesser<T>(in T t, in T? other)
            where T : struct, IComparable
            =>
                other.HasValue ? Lesser(t, other.Value) : t;

        internal static T Lesser<T>(in T t, in T other)
            where T : struct, IComparable
            =>
                t.CompareTo(other) < 0 ? t : other;

        internal static T Greater<T>(in T t, in T? other)
            where T : struct, IComparable
            =>
                other.HasValue ? Greater(t, other.Value) : t;

        internal static T Greater<T>(in T t, in T other)
            where T : struct, IComparable
            =>
                t.CompareTo(other) > 0 ? t : other;
    }
}
