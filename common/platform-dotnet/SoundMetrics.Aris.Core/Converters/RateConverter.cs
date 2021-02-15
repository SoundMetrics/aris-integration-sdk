// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace SoundMetrics.Aris.Core.Converters
{
    public sealed class RateConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string)
                || sourceType == typeof(float)
                || sourceType == typeof(double)
                || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string)
                || destinationType == typeof(float)
                || destinationType == typeof(double)
                || base.CanConvertTo(context, destinationType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return Rate.ToRate(Convert.ToDouble(value, culture));
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var rate = ((Rate)value).Hz;

            if (destinationType == typeof(string))
            {
                return rate.ToString("G", culture);

            }
            else if (destinationType == typeof(double))
            {
                return rate;
            }
            else if (destinationType == typeof(float))
            {
                return (float)rate;
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
