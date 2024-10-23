// Copyright (c) 2021-2024 Sound Metrics Corp.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace SoundMetrics.Aris.Core
{
#pragma warning disable CA2225 // Operator overloads have named alternates

    /// <summary>
    /// Temperature in degrees Celsius.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay}")]
    [DataContract]
    [JsonConverter(typeof(TemperatureJsonConverter))]
    public struct Temperature : IComparable, IComparable<Temperature>, IEquatable<Temperature>, IConvertible
    {
        [DataMember]
        private readonly double _degreesC;

        private Temperature(double degreesC)
        {
            _degreesC = degreesC;
        }

        public static explicit operator double(Temperature t) => t.DegreesCelsius;
        public static explicit operator Temperature(double t) => new Temperature(t);

        public static readonly Temperature Zero = new Temperature(degreesC: 0.0);

        public double DegreesCelsius => _degreesC;

        public override bool Equals(object? obj)
        {
            if (obj is Temperature t)
            {
                return Equals(t);
            }

            return false;
        }

        public bool Equals(Temperature other) => this._degreesC == other._degreesC;

        public override int GetHashCode()
        {
            return _degreesC.GetHashCode();
        }

        public static bool operator ==(Temperature a, Temperature b)
        {
            return a._degreesC == b._degreesC;
        }

        public static bool operator !=(Temperature a, Temperature b)
        {
            return !(a == b);
        }

        public static bool operator <(Temperature a, Temperature b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator <=(Temperature a, Temperature b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >(Temperature a, Temperature b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator >=(Temperature a, Temperature b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static Temperature Max(in Temperature a, in Temperature b) => a > b ? a : b;

        public static Temperature Min(in Temperature a, in Temperature b) => a < b ? a : b;

        public int CompareTo(Temperature other)
        {
            double difference = this._degreesC - other._degreesC;
            return difference < 0 ? -1 : (difference == 0 ? 0 : +1);
        }


        public int CompareTo(object? obj) 
            => obj is Temperature other 
                    ? CompareTo(other) 
                    : throw new ArgumentException($"Unexpected type: [{obj?.GetType().Name}]");

        public Temperature Abs()
        {
            return new Temperature(Math.Abs(_degreesC));
        }

        public static Temperature operator +(Temperature a, Temperature b)
        {
            return new Temperature(degreesC: a.DegreesCelsius + b.DegreesCelsius);
        }

        public static Temperature operator -(Temperature a, Temperature b)
        {
            return new Temperature(degreesC: a.DegreesCelsius - b.DegreesCelsius);
        }

        public static Temperature operator *(Temperature a, double b)
        {
            return new Temperature(degreesC: a.DegreesCelsius * b);
        }

        public static Temperature operator *(double a, Temperature b)
        {
            return new Temperature(degreesC: a * b.DegreesCelsius);
        }

        public static Temperature operator /(Temperature a, double b)
        {
            return new Temperature(degreesC: a.DegreesCelsius / b);
        }

        public static double operator /(Temperature a, Temperature b)
        {
            return a.DegreesCelsius / b.DegreesCelsius;
        }

        public override string ToString()
            => string.Format(CultureInfo.CurrentCulture, "{0:F3} \u00B0C", this.DegreesCelsius);

        private string DebuggerDisplay
            => string.Format(CultureInfo.CurrentCulture, "{0:F3} \u00B0C", this.DegreesCelsius);

        #region IConvertible

        TypeCode IConvertible.GetTypeCode() => TypeCode.Object;

        bool IConvertible.ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();

        byte IConvertible.ToByte(IFormatProvider? provider) => throw new InvalidCastException();

        char IConvertible.ToChar(IFormatProvider? provider) => throw new InvalidCastException();

        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new InvalidCastException();

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
            => Convert.ToDecimal(_degreesC, provider);

        double IConvertible.ToDouble(IFormatProvider? provider)
            => Convert.ToDouble(_degreesC, provider);

        short IConvertible.ToInt16(IFormatProvider? provider)
            => Convert.ToInt16(_degreesC, provider);

        int IConvertible.ToInt32(IFormatProvider? provider)
            => Convert.ToInt32(_degreesC, provider);

        long IConvertible.ToInt64(IFormatProvider? provider)
            => Convert.ToInt64(_degreesC, provider);

        sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw new InvalidCastException();

        float IConvertible.ToSingle(IFormatProvider? provider)
            => Convert.ToSingle(_degreesC, provider);

        string IConvertible.ToString(IFormatProvider? provider)
            => this.ToString();

        object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
        {
            if (conversionType == typeof(string))
            {
                return _degreesC.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
            => Convert.ToUInt16(_degreesC, provider);

        uint IConvertible.ToUInt32(IFormatProvider? provider)
            => Convert.ToUInt32(_degreesC, provider);

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
            => Convert.ToUInt64(_degreesC, provider);

        #endregion // IConvertible
    }

#pragma warning restore CA2225 // Operator overloads have named alternates
}
