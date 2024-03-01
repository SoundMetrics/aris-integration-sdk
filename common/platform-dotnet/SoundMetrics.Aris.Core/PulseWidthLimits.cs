// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core
{
    public sealed class PulseWidthLimits
    {
        internal PulseWidthLimits(
            in (int Min, int Max) limits,
            int narrow,
            int medium,
            int wide,
            double multiplier,
            int maxCumulativePulsePerSecond)
        {
            Limits = new InclusiveValueRange<FineDuration>(
                (FineDuration)limits.Min, (FineDuration)limits.Max);
            Narrow = (FineDuration)narrow;
            Medium = (FineDuration)medium;
            Wide = (FineDuration)wide;
            Multiplier = multiplier;
            MaxCumulativePulsePerSecond = (FineDuration)maxCumulativePulsePerSecond;
        }

        public InclusiveValueRange<FineDuration> Limits { get; }
        public FineDuration Narrow { get; }
        public FineDuration Medium { get; }
        public FineDuration Wide { get; }
        internal double Multiplier { get; }
        internal FineDuration MaxCumulativePulsePerSecond { get; }
    }
}
