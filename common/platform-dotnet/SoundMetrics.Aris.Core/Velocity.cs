// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{MetersPerSecond} m/s")]
    public struct Velocity : IComparable<Velocity>
    {
        private static readonly Velocity zero = new Velocity(Distance.FromMeters(0), FineDuration.FromSeconds(1));
        private readonly double _metersPerSecond;

        public Velocity(Distance distance, FineDuration time)
        {
            _metersPerSecond = distance.Meters / time.TotalSeconds;
        }

        public static Velocity Zero { get { return zero; } }
        public double MetersPerSecond { get { return _metersPerSecond; } }

        public static Velocity FromMetersPerSecond(double meters)
        {
            return new Velocity(Distance.FromMeters(meters), FineDuration.FromSeconds(1.0));
        }

        public override bool Equals(object obj)
        {
            Velocity? other = obj as Velocity?;
            if (!other.HasValue)
                return false;

            return this._metersPerSecond == other.Value._metersPerSecond;
        }

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
            return Distance.FromMeters(velocity.MetersPerSecond * time.TotalSeconds);
        }

        public static Distance operator *(FineDuration time, Velocity velocity)
        {
            return Distance.FromMeters(velocity.MetersPerSecond * time.TotalSeconds);
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
        {
            return string.Format("{0} m/s", this.MetersPerSecond);
        }
    }
}
