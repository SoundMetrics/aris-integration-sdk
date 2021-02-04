// Copyright (c) 2010-2021 Sound Metrics Corp.

using SoundMetrics.Aris.Core.Raw;
using System;

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

                        MaxPulseWidthLowFrequency = FineDuration.FromMicroseconds(40),
                        MaxPulseWidthHighFrequency = FineDuration.FromMicroseconds(30),
                        MaxCumulativePulsePerSecond = FineDuration.FromMicroseconds(300),
                    },

                    SmallPeriodAdjustmentFactor = 0.18,
                    LargePeriodAdjustmentFactor = 0.03,

                    MakeDefaultSettings = CreateSettingsBuilder(MakeDefaultSettings1800),
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

                        MaxPulseWidthLowFrequency = FineDuration.FromMicroseconds(24),
                        MaxPulseWidthHighFrequency = FineDuration.FromMicroseconds(16),
                        MaxCumulativePulsePerSecond = FineDuration.FromMicroseconds(240),
                    },

                    SmallPeriodAdjustmentFactor = 0.18,
                    LargePeriodAdjustmentFactor = 0.03,

                    MakeDefaultSettings = CreateSettingsBuilder(MakeDefaultSettings3000),
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

                        MaxPulseWidthLowFrequency = FineDuration.FromMicroseconds(80),
                        MaxPulseWidthHighFrequency = FineDuration.FromMicroseconds(60),
                        MaxCumulativePulsePerSecond = FineDuration.FromMicroseconds(400),
                    },

                    SmallPeriodAdjustmentFactor = 0.02,
                    LargePeriodAdjustmentFactor = 0.02,

                    MakeDefaultSettings = CreateSettingsBuilder(MakeDefaultSettings1200),
                };

            return configurations;

            Func<EnvironmentalContext, AcousticSettingsRaw>
                CreateSettingsBuilder(Func<EnvironmentalContext, AcousticSettingsRaw> makeDefaultSettings)
            {
                return MakeSettings;

                AcousticSettingsRaw MakeSettings(EnvironmentalContext environmentalContext)
                {
                    var defaultSettings = makeDefaultSettings(environmentalContext);
                    return WindowOperations.ToMediumWindow(defaultSettings);
                }
            }

            AcousticSettingsRaw MakeDefaultSettings1800(EnvironmentalContext sonarEnvironment)
            {
                var systemType = SystemType.Aris1800;
                var pingMode = PingMode.PingMode3;
                var frequency = Frequency.High;
                var sampleCount = 1250;
                var sampleStartDelay = FineDuration.FromMicroseconds(2000);
                var samplePeriod = FineDuration.FromMicroseconds(17);
                var pulseWidth = FineDuration.FromMicroseconds(14);
                var receiverGain = 18;
                var antiAliasing = FineDuration.Zero;
                var interpacketDelay = new InterpacketDelaySettings { Enable = false };
                var enableTransmit = true;
                var enable150Volts = true;
                var focusPosition = FocusPosition.Automatic;

                // We get away with referencing the system configuration here (while we're
                // defining system configurations) as we're returning this function via
                // `CreateSettingsBuilder` above for later invocation.
                var maxFrameRate =
                    MaxFrameRate.DetermineMaximumFrameRate(
                        SystemConfiguration.GetConfiguration(systemType),
                        pingMode,
                        sampleCount,
                        sampleStartDelay,
                        samplePeriod,
                        antiAliasing,
                        interpacketDelay,
                        out FineDuration cyclePeriod);

                return new AcousticSettingsRaw(
                    systemType, maxFrameRate, sampleCount, sampleStartDelay, cyclePeriod, samplePeriod,
                    pulseWidth, pingMode, enableTransmit, frequency, enable150Volts, receiverGain,
                    focusPosition, antiAliasing, interpacketDelay, sonarEnvironment);
            }

            AcousticSettingsRaw MakeDefaultSettings3000(EnvironmentalContext sonarEnvironment)
            {
                var systemType = SystemType.Aris3000;
                var pingMode = PingMode.PingMode9;
                var frequency = Frequency.High;
                var sampleCount = 1250;
                var sampleStartDelay = FineDuration.FromMicroseconds(1300);
                var samplePeriod = FineDuration.FromMicroseconds(5);
                var pulseWidth = FineDuration.FromMicroseconds(5);
                var receiverGain = 12;
                var antiAliasing = FineDuration.Zero;
                var interpacketDelay = new InterpacketDelaySettings { Enable = false };
                var enableTransmit = true;
                var enable150Volts = true;
                var focusPosition = FocusPosition.Automatic;

                // We get away with referencing the system configuration here (while we're
                // defining system configurations) as we're returning this function via
                // `CreateSettingsBuilder` above for later invocation.
                var maxFrameRate =
                    MaxFrameRate.DetermineMaximumFrameRate(
                        SystemConfiguration.GetConfiguration(systemType),
                        pingMode,
                        sampleCount,
                        sampleStartDelay,
                        samplePeriod,
                        antiAliasing,
                        interpacketDelay,
                        out FineDuration cyclePeriod);

                return new AcousticSettingsRaw(
                    systemType, maxFrameRate, sampleCount, sampleStartDelay, cyclePeriod, samplePeriod,
                    pulseWidth, pingMode, enableTransmit, frequency, enable150Volts, receiverGain,
                    focusPosition, antiAliasing, interpacketDelay, sonarEnvironment);
            }

            AcousticSettingsRaw MakeDefaultSettings1200(EnvironmentalContext sonarEnvironment)
            {
                var systemType = SystemType.Aris1200;
                var pingMode = PingMode.PingMode1;
                var frequency = Frequency.High;
                var sampleCount = 1250;
                var sampleStartDelay = FineDuration.FromMicroseconds(4000);
                var samplePeriod = FineDuration.FromMicroseconds(28);
                var pulseWidth = FineDuration.FromMicroseconds(24);
                var receiverGain = 20;
                var antiAliasing = FineDuration.Zero;
                var interpacketDelay = new InterpacketDelaySettings { Enable = false };
                var enableTransmit = true;
                var enable150Volts = true;
                var focusPosition = FocusPosition.Automatic;

                // We get away with referencing the system configuration here (while we're
                // defining system configurations) as we're returning this function via
                // `CreateSettingsBuilder` above for later invocation.
                var maxFrameRate =
                    MaxFrameRate.DetermineMaximumFrameRate(
                        SystemConfiguration.GetConfiguration(systemType),
                        pingMode,
                        sampleCount,
                        sampleStartDelay,
                        samplePeriod,
                        antiAliasing,
                        interpacketDelay,
                        out FineDuration cyclePeriod);

                return new AcousticSettingsRaw(
                    systemType, maxFrameRate, sampleCount, sampleStartDelay, cyclePeriod, samplePeriod,
                    pulseWidth, pingMode, enableTransmit, frequency, enable150Volts, receiverGain,
                    focusPosition, antiAliasing, interpacketDelay, sonarEnvironment);
            }
        }
    }
}
