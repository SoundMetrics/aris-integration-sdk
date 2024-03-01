﻿// Copyright (c) 2010-2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core
{
    using SoundMetrics.Aris.Core.Raw;
    using System;
    using static System.Math;
    using static SoundMetrics.Aris.Core.FineDuration;
    using static SoundMetrics.Aris.Core.Rate;

    public sealed class SystemConfigurationRaw
    {
        internal SystemConfigurationRaw() {}

        public InclusiveValueRange<int> FocusPositionLimits { get; internal set; }
        public InclusiveValueRange<FineDuration> SampleStartDelayLimits { get; internal set; }
        public InclusiveValueRange<FineDuration> CyclePeriodLimits { get; internal set; }
        public InclusiveValueRange<FineDuration> SamplePeriodLimits { get; internal set; }
        //public InclusiveValueRange<FineDuration> PulseWidthLimits { get; internal set; }

        internal PulseWidthLimits PulseWidthLimitsLowFrequency { get; set; }
        internal PulseWidthLimits PulseWidthLimitsHighFrequency { get; set; }

        //internal FineDuration MaxPulseWidthLowFrequency { get; set; }
        //internal FineDuration MaxPulseWidthHighFrequency { get; set; }
        //internal FineDuration MaxCumulativePulsePerSecond { get; set; }
        //internal double PulseWidthMultiplierLow { get; set; }
        //internal double PulseWidthMultiplierHigh { get; set; }

        public PulseWidthLimits GetPulseWidthLimitsFor(Frequency frequency)
            => frequency == Frequency.Low
                ? PulseWidthLimitsLowFrequency
                : PulseWidthLimitsHighFrequency;

        internal double GetPulseWidthMultiplierFor(Frequency frequency)
            => GetPulseWidthLimitsFor(frequency).Multiplier;

        internal InclusiveValueRange<FineDuration> AllowedPulseWidthRangeFor(Frequency frequency)
            => GetPulseWidthLimitsFor(frequency).Limits;

        public FineDuration LimitPulseWidthEnergy(Frequency frequency, FineDuration pulseWidth, Rate frameRate)
        {
            var values = GetPulseWidthLimitsFor(frequency);
            var maxEnergyAllowedPulseWidth =
                values.MaxCumulativePulsePerSecond / frameRate.Hz;

            var limitedPulseWidth =
                Min(Min(pulseWidth, values.Limits.Maximum),
                    maxEnergyAllowedPulseWidth);

            return limitedPulseWidth;
        }

        public Rate LimitFrameRateEnergy(
            Frequency frequency,
            FineDuration pulseWidth,
            Rate frameRate)
        {
            var values = GetPulseWidthLimitsFor(frequency);
            var maxEnergyAllowedFrameRate =
                (Rate)(values.MaxCumulativePulsePerSecond.TotalMicroseconds
                            / pulseWidth.TotalMicroseconds);

            var limitedFrameRate = Min(maxEnergyAllowedFrameRate, frameRate);
            return limitedFrameRate;
        }

        public FineDuration MaxAntiAliasingFor(AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            return CyclePeriodLimits.Maximum - settings.CyclePeriod;
        }

        public static readonly FineDuration CyclePeriodMargin = (FineDuration)420;
        public const int MinAntialiasing = 0;

        public static readonly InclusiveValueRange<int> ReceiverGainLimits = new InclusiveValueRange<int>(0, 24);
        public static readonly InclusiveValueRange<double> FrameRateLimits = new InclusiveValueRange<double>(1.0, 15.0);

        public static readonly InclusiveValueRange<double> WindowStartLimits = new InclusiveValueRange<double>(0.7, 40.0);
        public static readonly InclusiveValueRange<double> WindowEndLimits = new InclusiveValueRange<double>(1.3, 100.0);

    }
}
