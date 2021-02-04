// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    /// <summary>
    /// Distance in meters.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay} m")]
    public struct Distance : IComparable<Distance>, IEquatable<Distance>, IConvertible
    {
        private readonly double _meters;

        private Distance(double meters)
        {
            _meters = meters;
        }

        public static Distance FromMeters(double meters)
        {
            return new Distance(meters);
        }

        public static Distance Zero = new Distance(0.0);

        public double Meters { get { return _meters; } }
        public double Centimeters { get { return _meters * 100; } }
        public double Millimeters { get { return _meters * 1000; } }
        public double Microns { get { return _meters * (1000 * 1000); } }
        public double Nanometers { get { return _meters * (1000 * 1000 * 1000); } }
        public bool IsPositive { get { return _meters > 0.0; } }
        public bool IsNegative { get { return _meters < 0.0; } }

        /// <summary>
        /// Performs a Floor() on Distance's native unit, which is meters.
        /// </summary>
        public Distance Floor => Distance.FromMeters(Math.Floor(_meters));

        /// <summary>
        /// Performs a Ceiling() on Distance's native unit, which is meters.
        /// </summary>
        public Distance Ceiling => Distance.FromMeters(Math.Ceiling(_meters));

        public override bool Equals(object obj)
        {
            if (obj is Distance)
            {
                return Equals((Distance)obj);
            }

            return false;
        }

        public bool Equals(Distance other) => this._meters == other._meters;

        public override int GetHashCode()
        {
            return _meters.GetHashCode();
        }

        public static bool operator ==(Distance a, Distance b)
        {
            return a._meters == b._meters;
        }

        public static bool operator !=(Distance a, Distance b)
        {
            return !(a == b);
        }

        public static bool operator <(Distance a, Distance b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator <=(Distance a, Distance b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >(Distance a, Distance b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator >=(Distance a, Distance b)
        {
            return a.CompareTo(b) >= 0;
        }

        public int CompareTo(Distance other)
        {
            double difference = this._meters - other._meters;
            return difference < 0 ? -1 : (difference == 0 ? 0 : +1);
        }

        public Distance Abs()
        {
            return new Distance(Math.Abs(_meters));
        }

        public static Distance operator +(Distance a, Distance b)
        {
            return new Distance(meters: a.Meters + b.Meters);
        }

        public static Distance operator -(Distance a, Distance b)
        {
            return new Distance(meters: a.Meters - b.Meters);
        }

        public static Distance operator *(Distance a, double b)
        {
            return new Distance(meters: a.Meters * b);
        }

        public static Distance operator *(double a, Distance b)
        {
            return new Distance(meters: a * b.Meters);
        }

        public static Distance operator /(Distance a, double b)
        {
            return new Distance(meters: a.Meters / b);
        }

        public static double operator /(Distance a, Distance b)
        {
            return a.Meters / b.Meters;
        }

        public static Velocity operator /(Distance distance, FineDuration time)
        {
            return new Velocity(distance, time);
        }

        public static Distance Min(Distance a, Distance b)
        {
            return a < b ? a : b;
        }

        public static Distance Max(Distance a, Distance b)
        {
            return a > b ? a : b;
        }

        public override string ToString()
        {
            return string.Format("{0:F3} m", this.Meters);
        }

        private string DebuggerDisplay { get { return string.Format("{0:F3} m", this.Meters); } }

        #region IConvertible

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(this.Meters);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return this.Meters;
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(this.Meters);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(this.Meters);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(this.Meters);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(this.Meters);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return this.ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(this.Meters);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(this.Meters);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(this.Meters);
        }

        #endregion IConvertible
    }
}
