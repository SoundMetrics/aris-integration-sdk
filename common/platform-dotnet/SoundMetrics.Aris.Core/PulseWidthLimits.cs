// Copyright (c) 2022-2024 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core
{
    public record struct PulseWidthLimits(
        in InclusiveValueRange<FineDuration> Limits,
        in FineDuration Narrow,
        in FineDuration Medium,
        in FineDuration Wide,
        in double Multiplier,
        in FineDuration MaxCumulativePulsePerSecond)
    {
        public PulseWidthLimits(
            in (int Min, int Max) limits,
            int narrow,
            int medium,
            int wide,
            double multiplier,
            int maxCumulativePulsePerSecond)
            : this(
                Limits: new InclusiveValueRange<FineDuration>(
                    (FineDuration) limits.Min, (FineDuration) limits.Max),
                Narrow: (FineDuration) narrow,
                Medium: (FineDuration)medium,
                Wide: (FineDuration)wide,
                Multiplier: multiplier,
                MaxCumulativePulsePerSecond: (FineDuration)maxCumulativePulsePerSecond
            )
        {
        }
    }
}
