// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;
using System.Diagnostics;
using System.Globalization;

namespace SoundMetrics.Aris.Core
{
#pragma warning disable CA2225 // Operator overloads have named alternates

    [DebuggerDisplay("{MetersPerSecond} m/s")]
    public struct Velocity : IComparable<Velocity>, IEquatable<Velocity>
    {
        private static readonly Velocity zero =
            new Velocity((Distance)0, FineDuration.FromSeconds(1));

        private readonly double _metersPerSecond;

        public Velocity(Distance distance, FineDuration time)
        {
            _metersPerSecond = distance.Meters / time.TotalSeconds;
        }

        public static explicit operator double(Velocity v) => v._metersPerSecond;
        public static explicit operator Velocity(double v) => Velocity.FromMetersPerSecond(v);

        public static Velocity Zero { get { return zero; } }
        public double MetersPerSecond { get { return _metersPerSecond; } }

        public static Velocity FromMetersPerSecond(double meters)
            => new Velocity((Distance)meters, FineDuration.FromSeconds(1.0));

        public override bool Equals(object obj)
            => obj is Velocity other && this.Equals(other);

        public bool Equals(Velocity other)
            => this._metersPerSecond == other._metersPerSecond;

        public override int GetHashCode()
        {
            return _metersPerSecond.GetHashCode();
        }


        public static bool operator ==(Velocity a, Velocity b)
        {
            return a._metersPerSecond == b._metersPerSecond;
        }

        public static bool operator !=(Velocity a, Velocity b)
        {
            return !(a == b);
        }

        public static bool operator <(Velocity a, Velocity b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator <=(Velocity a, Velocity b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >(Velocity a, Velocity b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator >=(Velocity a, Velocity b)
        {
            return a.CompareTo(b) >= 0;
        }

        public int CompareTo(Velocity other)
        {
            double difference = this._metersPerSecond - other._metersPerSecond;
            return difference < 0 ? -1 : (difference == 0 ? 0 : +1);
        }

        public static Distance operator *(Velocity velocity, FineDuration time)
        {
            return (Distance)(velocity.MetersPerSecond * time.TotalSeconds);
        }

        public static Distance operator *(FineDuration time, Velocity velocity)
        {
            return (Distance)(velocity.MetersPerSecond * time.TotalSeconds);
        }

        public static Velocity operator *(Velocity velocity, double multiplier)
            => Velocity.FromMetersPerSecond(velocity.MetersPerSecond * multiplier);

        public static Velocity operator *(double multiplier, Velocity velocity)
            => Velocity.FromMetersPerSecond(multiplier * velocity.MetersPerSecond);

        public static Velocity operator /(Velocity velocity, double divisor)
        {
            return FromMetersPerSecond(velocity.MetersPerSecond / divisor);
        }

        public static FineDuration operator /(Distance distance, Velocity velocity)
        {
            // m / m/s => s
            return FineDuration.FromSeconds(distance.Meters / velocity.MetersPerSecond);
        }

        public override string ToString()
            => string.Format(CultureInfo.CurrentCulture, "{0:F3} m/s", this.MetersPerSecond);
    }

#pragma warning restore CA2225 // Operator overloads have named alternates
}
