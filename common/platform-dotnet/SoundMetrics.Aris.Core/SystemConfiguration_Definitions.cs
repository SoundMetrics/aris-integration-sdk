// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core
{
    public sealed partial class SystemConfiguration
    {
        private static SystemConfiguration[] InitializeConfigurations()
        {
            var commonSampleCountRange = new ValueRange<int>(200, 4000);
            var commonReceiverGainRange = new ValueRange<int>(0, 24);
            var commonFrameRateRange = new ValueRange<Rate>(Rate.PerSecond(1), Rate.PerSecond(15));

            var configurations = new SystemConfiguration[3];

            configurations[SystemType.Aris1800.IntegralValue] =
                new SystemConfiguration
                {
                    PingModes = new[] { PingMode.PingMode1, PingMode.PingMode3 },
                    DefaultPingMode = PingMode.PingMode3,
                    SampleCountRange = commonSampleCountRange,
                    ReceiverGainRange = commonReceiverGainRange,
                    FrameRateRange = commonFrameRateRange,
                    WindowStartRange = RangeOfMeters(0.7, 25.0),
                    WindowEndRange = RangeOfMeters(1.3, 50.0),

                    FrequencyCrossover = Distance.FromMeters(15.0),
                    UsefulLowFrequencyImagingRange = RangeOfMeters(20.0, 50.0),
                    UsefulHighFrequencyImagingRange = RangeOfMeters(5.0, 20.0),

                    RawConfiguration = new SystemConfigurationRaw
                    {
                        PulseWidthRange = RangeOfDuration(4, 40),
                        SampleStartDelayRange = RangeOfDuration(930, 36_000),
                        SamplePeriodRange = RangeOfDuration(4, 32),
                        FocusPositionRange = new ValueRange<int>(0, 1000),
                        CyclePeriodRange = RangeOfDuration(1802, 80_000),
                    },

                    SmallPeriodAdjustmentFactor = 0.18,
                    LargePeriodAdjustmentFactor = 0.03,
                };

            configurations[SystemType.Aris3000.IntegralValue] =
                new SystemConfiguration
                {
                    PingModes = new[] { PingMode.PingMode6, PingMode.PingMode9 },
                    DefaultPingMode = PingMode.PingMode9,
                    SampleCountRange = commonSampleCountRange,
                    ReceiverGainRange = commonReceiverGainRange,
                    FrameRateRange = commonFrameRateRange,
                    WindowStartRange = RangeOfMeters(0.7, 12.0),
                    WindowEndRange = RangeOfMeters(1.3, 20.0),

                    FrequencyCrossover = Distance.FromMeters(5.0),
                    UsefulLowFrequencyImagingRange = RangeOfMeters(8.0, 20.0),
                    UsefulHighFrequencyImagingRange = RangeOfMeters(1.0, 8.0),

                    RawConfiguration = new SystemConfigurationRaw
                    {
                        PulseWidthRange = RangeOfDuration(4, 24),
                        SampleStartDelayRange = RangeOfDuration(930, 18_000),
                        SamplePeriodRange = RangeOfDuration(4, 26),
                        FocusPositionRange = new ValueRange<int>(0, 1000),
                        CyclePeriodRange = RangeOfDuration(1802, 40_000),
                    },

                    SmallPeriodAdjustmentFactor = 0.18,
                    LargePeriodAdjustmentFactor = 0.03,
                };

            configurations[SystemType.Aris1200.IntegralValue] =
                new SystemConfiguration
                {
                    PingModes = new[] { PingMode.PingMode1 },
                    DefaultPingMode = PingMode.PingMode1,
                    SampleCountRange = commonSampleCountRange,
                    ReceiverGainRange = commonReceiverGainRange,
                    FrameRateRange = commonFrameRateRange,
                    WindowStartRange = RangeOfMeters(0.7, 40.0),
                    WindowEndRange = RangeOfMeters(1.3, 100.0),

                    FrequencyCrossover = Distance.FromMeters(25.0),
                    UsefulLowFrequencyImagingRange = RangeOfMeters(30.0, 100.0),
                    UsefulHighFrequencyImagingRange = RangeOfMeters(10.0, 30.0),

                    RawConfiguration = new SystemConfigurationRaw
                    {
                        PulseWidthRange = RangeOfDuration(4, 80),
                        SampleStartDelayRange = RangeOfDuration(930, 60_000),
                        SamplePeriodRange = RangeOfDuration(4, 40),
                        FocusPositionRange = new ValueRange<int>(0, 1000),
                        CyclePeriodRange = RangeOfDuration(1802, 150_000),
                    },

                    SmallPeriodAdjustmentFactor = 0.02,
                    LargePeriodAdjustmentFactor = 0.02,
                };

            return configurations;
        }
    }
}
