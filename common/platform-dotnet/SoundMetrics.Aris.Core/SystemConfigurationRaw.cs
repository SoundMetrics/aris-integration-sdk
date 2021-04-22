// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core
{
    using SoundMetrics.Aris.Core.Raw;
    using System;
    using static FineDuration;
    using static Rate;

    public sealed class SystemConfigurationRaw
    {
        internal SystemConfigurationRaw() {}

        public ValueRange<int> FocusPositionRange { get; internal set; }
        public ValueRange<FineDuration> SampleStartDelayRange { get; internal set; }
        public ValueRange<FineDuration> CyclePeriodRange { get; internal set; }
        public ValueRange<FineDuration> SamplePeriodRange { get; internal set; }
        public ValueRange<FineDuration> PulseWidthRange { get; internal set; }

        internal FineDuration MaxPulseWidthLowFrequency { get; set; }
        internal FineDuration MaxPulseWidthHighFrequency { get; set; }
        internal FineDuration MaxCumulativePulsePerSecond { get; set; }
        internal double PulseWidthMultiplierLow { get; set; }
        internal double PulseWidthMultiplierHigh { get; set; }

        internal double GetPulseWidthMultiplierFor(Frequency frequency)
            => frequency == Frequency.Low
                    ? PulseWidthMultiplierLow
                    : PulseWidthMultiplierHigh;

        internal ValueRange<FineDuration> AllowedPulseWidthRangeFor(Frequency frequency)
            => new ValueRange<FineDuration>(
                    PulseWidthRange.Minimum,
                    frequency == Frequency.Low
                        ? MaxPulseWidthLowFrequency
                        : MaxPulseWidthHighFrequency);

        public FineDuration LimitPulseWidthEnergy(Frequency frequency, FineDuration pulseWidth, Rate frameRate)
        {
            var maxPulseWidthForFrequency = frequency == Frequency.Low ? MaxPulseWidthLowFrequency : MaxPulseWidthHighFrequency;
            var maxEnergyAllowedPulseWidth = MaxCumulativePulsePerSecond / frameRate.Hz;


            var limitedPulseWidth =
                Min(Min(pulseWidth, maxPulseWidthForFrequency), maxEnergyAllowedPulseWidth);

            return limitedPulseWidth;
        }

        public Rate LimitFrameRateEnergy(FineDuration pulseWidth, Rate frameRate)
        {
            var maxEnergyAllowedFrameRate = (Rate)(MaxCumulativePulsePerSecond.TotalMicroseconds / pulseWidth.TotalMicroseconds);

            var limitedFrameRate = Min(maxEnergyAllowedFrameRate, frameRate);
            return limitedFrameRate;
        }

        public FineDuration MaxAntialiasingFor(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            return CyclePeriodRange.Maximum - settings.CyclePeriod;
        }

        public static readonly FineDuration CyclePeriodMargin = FineDuration.FromMicroseconds(420);
        public const int MinAntialiasing = 0;

        public static readonly ValueRange<int> MaxSampleCountRange = new ValueRange<int>(200, 4000);
        public static readonly ValueRange<int> MaxFocusPositionRange = new ValueRange<int>(0, 1000);
        public static readonly ValueRange<int> MaxReceiverGainRange = new ValueRange<int>(0, 24);
        public static readonly ValueRange<double> MaxFrameRateRange = new ValueRange<double>(1.0, 15.0);

        public static readonly ValueRange<int> MaxSampleStartDelayRange = new ValueRange<int>(930, 60000);
        public static readonly ValueRange<int> MaxCyclePeriodRange = new ValueRange<int>(1802, 150000);
        public static readonly ValueRange<int> MaxSamplePeriodRange = new ValueRange<int>(4, 100);
        public static readonly ValueRange<int> MaxPulseWidthRange = new ValueRange<int>(4, 80);

        public static readonly ValueRange<double> MaxWindowStartRange = new ValueRange<double>(0.7, 40.0);
        public static readonly ValueRange<double> MaxWindowEndRange = new ValueRange<double>(1.3, 100.0);

    }
}
