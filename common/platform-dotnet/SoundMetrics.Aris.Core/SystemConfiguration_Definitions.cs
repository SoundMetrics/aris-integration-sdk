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
            var commonFrameRateRange = new ValueRange<Rate>(Rate.ToRate(1), Rate.ToRate(15));

            var configurations = new SystemConfiguration[3];

            configurations[SystemType.Aris1800.IntegralValue] =
                new SystemConfiguration
                {
                    AvailablePingModes = new[] { PingMode.PingMode1, PingMode.PingMode3 },
                    DefaultPingMode = PingMode.PingMode3,
                    SampleCountRange = commonSampleCountRange,
                    ReceiverGainRange = commonReceiverGainRange,
                    FrameRateRange = commonFrameRateRange,
                    WindowStartRange = RangeOfMeters(0.7, 25.0),
                    WindowEndRange = RangeOfMeters(1.3, 50.0),

                    FrequencyCrossover = (Distance)15.0,

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
                        PulseWidthMultiplierLow = 1.0,
                        PulseWidthMultiplierHigh = 1.5,
                    },

                    SmallPeriodAdjustmentFactor = 0.18,
                    LargePeriodAdjustmentFactor = 0.03,

                    MakeDefaultSettings = CreateSettingsBuilder(MakeDefaultSettings1800),

                    FrequencyHigh = Rate.ToRate(1_800_000),
                    FrequencyLow = Rate.ToRate(1_100_000),
                };

            configurations[SystemType.Aris3000.IntegralValue] =
                new SystemConfiguration
                {
                    AvailablePingModes = new[] { PingMode.PingMode6, PingMode.PingMode9 },
                    DefaultPingMode = PingMode.PingMode9,
                    SampleCountRange = commonSampleCountRange,
                    ReceiverGainRange = commonReceiverGainRange,
                    FrameRateRange = commonFrameRateRange,
                    WindowStartRange = RangeOfMeters(0.7, 12.0),
                    WindowEndRange = RangeOfMeters(1.3, 20.0),

                    FrequencyCrossover = (Distance)5.0,

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
                        PulseWidthMultiplierLow = 1.5,
                        PulseWidthMultiplierHigh = 2.0,
                    },

                    SmallPeriodAdjustmentFactor = 0.18,
                    LargePeriodAdjustmentFactor = 0.03,

                    MakeDefaultSettings = CreateSettingsBuilder(MakeDefaultSettings3000),

                    FrequencyHigh = Rate.ToRate(3_000_000),
                    FrequencyLow = Rate.ToRate(1_800_000),
                };

            configurations[SystemType.Aris1200.IntegralValue] =
                new SystemConfiguration
                {
                    AvailablePingModes = new[] { PingMode.PingMode1 },
                    DefaultPingMode = PingMode.PingMode1,
                    SampleCountRange = commonSampleCountRange,
                    ReceiverGainRange = commonReceiverGainRange,
                    FrameRateRange = commonFrameRateRange,
                    WindowStartRange = RangeOfMeters(0.7, 40.0),
                    WindowEndRange = RangeOfMeters(1.3, 100.0),

                    FrequencyCrossover = (Distance)25.0,

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
                        PulseWidthMultiplierLow = 1.0,
                        PulseWidthMultiplierHigh = 1.0, // high/low are the same for the 1200
                    },

                    SmallPeriodAdjustmentFactor = 0.02,
                    LargePeriodAdjustmentFactor = 0.02, // large/small are the same for the 1200

                    MakeDefaultSettings = CreateSettingsBuilder(MakeDefaultSettings1200),

                    FrequencyHigh = Rate.ToRate(1_200_000),
                    FrequencyLow = Rate.ToRate(700_000),
                };

            return configurations;

            Func<ObservedConditions, AcousticSettingsRaw>
                CreateSettingsBuilder(
                    Func<ObservedConditions, AcousticSettingsRaw> makeDefaultSettings)
            {
                return MakeSettings;

                AcousticSettingsRaw MakeSettings(ObservedConditions observedConditions)
                {
                    var defaultSettings = makeDefaultSettings(observedConditions);
                    return WindowOperations.ToMediumWindow(
                        defaultSettings,
                        observedConditions,
                        useMaxFrameRate: true,
                        useAutoFrequency: true);
                }
            }

            AcousticSettingsRaw MakeDefaultSettings1800(ObservedConditions observedConditions)
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
                var focusPosition = (Distance)10;
                var salinity = Salinity.Brackish;

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
                        interpacketDelay);

                return new AcousticSettingsRaw(
                    systemType, maxFrameRate, sampleCount, sampleStartDelay, samplePeriod,
                    pulseWidth, pingMode, enableTransmit, frequency, enable150Volts, receiverGain,
                    focusPosition, antiAliasing, interpacketDelay, salinity)
                    .WithAutomaticSettings(
                        observedConditions,
                        AutomaticAcousticSettings.All);
            }

            AcousticSettingsRaw MakeDefaultSettings3000(ObservedConditions observedConditions)
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
                var focusPosition = (Distance)10;
                var salinity = Salinity.Brackish;

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
                        interpacketDelay);

                return new AcousticSettingsRaw(
                    systemType, maxFrameRate, sampleCount, sampleStartDelay, samplePeriod,
                    pulseWidth, pingMode, enableTransmit, frequency, enable150Volts, receiverGain,
                    focusPosition, antiAliasing, interpacketDelay, salinity)
                    .WithAutomaticSettings(
                        observedConditions,
                        AutomaticAcousticSettings.All);
            }

            AcousticSettingsRaw MakeDefaultSettings1200(ObservedConditions observedConditions)
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
                var focusPosition = (Distance)10;
                var salinity = Salinity.Brackish;

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
                        interpacketDelay);

                return new AcousticSettingsRaw(
                    systemType, maxFrameRate, sampleCount, sampleStartDelay, samplePeriod,
                    pulseWidth, pingMode, enableTransmit, frequency, enable150Volts, receiverGain,
                    focusPosition, antiAliasing, interpacketDelay, salinity)
                    .WithAutomaticSettings(
                        observedConditions,
                        AutomaticAcousticSettings.All);
            }
        }
    }
}
