// Copyright (c) 2010-2022 Sound Metrics Corp.

using SoundMetrics.Aris.Core.Raw;
using System;

namespace SoundMetrics.Aris.Core
{
    public sealed partial class SystemConfiguration
    {
        private static SystemConfiguration[] InitializeConfigurations()
        {
            var commonReceiverGainLimits = new InclusiveValueRange<int>(0, 24);
            var commonFrameRateLimits = new InclusiveValueRange<Rate>(Rate.ToRate(1), Rate.ToRate(15));

            var configurations = new SystemConfiguration[3];

            configurations[SystemType.Aris1800.IntegralValue] =
                new SystemConfiguration(SystemType.Aris1800)
                {
                    AvailablePingModes = new[] { PingMode.PingMode1, PingMode.PingMode3 },
                    DefaultPingMode = PingMode.PingMode3,
                    SupportsTelephotoLens = true,
                    ReceiverGainLimits = commonReceiverGainLimits,
                    FrameRateLimits = commonFrameRateLimits,
                    WindowLimits = RangeOfMeters(0.7, 50.0),

                    RawConfiguration = new SystemConfigurationRaw
                    {
                        SampleStartDelayLimits = RangeOfDuration(930, 36_000),
                        SamplePeriodLimits = RangeOfDuration(4, 32),
                        FocusPositionLimits = focusPositionDeviceLimits,
                        CyclePeriodLimits = RangeOfDuration(1802, 80_000),

                        PulseWidthLimitsHighFrequency =
                            new PulseWidthLimits(
                                limits: (6, 24),
                                narrow: 8,
                                medium: 16,
                                wide: 24,
                                multiplier: 1.5,
                                maxCumulativePulsePerSecond: 300),
                        PulseWidthLimitsLowFrequency =
                            new PulseWidthLimits(
                                limits: (6, 40),
                                narrow: 12,
                                medium: 16,
                                wide: 24,
                                multiplier: 1.0,
                                maxCumulativePulsePerSecond: 300),
                    },

                    DefaultReceiverGain = 18f,
                    DefaultWindow = new((Distance)3.0, (Distance)20.0),
                    FrequencyHigh = Rate.ToRate(1_800_000),
                    FrequencyLow = Rate.ToRate(1_100_000),
                };

            configurations[SystemType.Aris3000.IntegralValue] =
                new SystemConfiguration(SystemType.Aris3000)
                {
                    AvailablePingModes = new[] { PingMode.PingMode6, PingMode.PingMode9 },
                    DefaultPingMode = PingMode.PingMode9,
                    SupportsTelephotoLens = false,
                    ReceiverGainLimits = commonReceiverGainLimits,
                    FrameRateLimits = commonFrameRateLimits,
                    WindowLimits = RangeOfMeters(0.7, 20.0),

                    RawConfiguration = new SystemConfigurationRaw
                    {
                        SampleStartDelayLimits = RangeOfDuration(930, 18_000),
                        SamplePeriodLimits = RangeOfDuration(4, 26),
                        FocusPositionLimits = focusPositionDeviceLimits,
                        CyclePeriodLimits = RangeOfDuration(1802, 40_000),

                        PulseWidthLimitsHighFrequency =
                            new PulseWidthLimits(
                                limits: (6, 16),
                                narrow: 5,
                                medium: 10,
                                wide: 16,
                                multiplier: 2.0,
                                maxCumulativePulsePerSecond: 240),
                        PulseWidthLimitsLowFrequency =
                            new PulseWidthLimits(
                                limits: (6, 24),
                                narrow: 8,
                                medium: 16,
                                wide: 24,
                                multiplier: 1.5,
                                maxCumulativePulsePerSecond: 240),
                    },

                    DefaultReceiverGain = 12f,
                    DefaultWindow = new((Distance)2.0, (Distance)10.0),
                    FrequencyHigh = Rate.ToRate(3_000_000),
                    FrequencyLow = Rate.ToRate(1_800_000),
                };

            configurations[SystemType.Aris1200.IntegralValue] =
                new SystemConfiguration(SystemType.Aris1200)
                {
                    AvailablePingModes = new[] { PingMode.PingMode1 },
                    DefaultPingMode = PingMode.PingMode1,
                    SupportsTelephotoLens = true,
                    ReceiverGainLimits = commonReceiverGainLimits,
                    FrameRateLimits = commonFrameRateLimits,
                    WindowLimits = RangeOfMeters(0.7, 100.0),

                    RawConfiguration = new SystemConfigurationRaw
                    {
                        SampleStartDelayLimits = RangeOfDuration(930, 60_000),
                        SamplePeriodLimits = RangeOfDuration(4, 40),
                        FocusPositionLimits = focusPositionDeviceLimits,
                        CyclePeriodLimits = RangeOfDuration(1802, 150_000),

                        PulseWidthLimitsHighFrequency =
                            new PulseWidthLimits(
                                limits: (8, 60),
                                narrow: 12,
                                medium: 24,
                                wide: 40,
                                multiplier: 1.0,
                                maxCumulativePulsePerSecond: 400),
                        PulseWidthLimitsLowFrequency =
                            new PulseWidthLimits(
                                limits: (8, 80),
                                narrow: 12,
                                medium: 24,
                                wide: 50,
                                multiplier: 1.0,
                                maxCumulativePulsePerSecond: 400),
                    },

                    DefaultReceiverGain = 20f,
                    DefaultWindow = new((Distance)4.0, (Distance)40.0),
                    FrequencyHigh = Rate.ToRate(1_200_000),
                    FrequencyLow = Rate.ToRate(700_000),
                };

            return configurations;
        }
    }
}
