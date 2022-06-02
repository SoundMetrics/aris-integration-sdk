// Copyright (c) 2010-2022 Sound Metrics Corp.

using SoundMetrics.Aris.Core.Raw;
using System;

namespace SoundMetrics.Aris.Core
{
    public sealed partial class SystemConfiguration
    {
        private static readonly ValueRange<int> sampleCountDeviceLimits
            = new ValueRange<int>(200, 4000);
        private static readonly ValueRange<int> pulseWidthDeviceLimits
            = new ValueRange<int>(4, 80);
        private static readonly ValueRange<int> sampleStartDelayDeviceLimits
            = new ValueRange<int>(930, 60000);
        private static readonly ValueRange<int> samplePeriodDeviceLimits
            = new ValueRange<int>(4, 100);
        private static readonly ValueRange<int> focusPositionDeviceLimits
            = new ValueRange<int>(0, 1000);
        private static readonly ValueRange<int> cyclePeriodDeviceLimits
            = new ValueRange<int>(1802, 150000);

        private static SystemConfiguration[] InitializeConfigurations()
        {
            var commonReceiverGainLimits = new ValueRange<int>(0, 24);
            var commonFrameRateLimits = new ValueRange<Rate>(Rate.ToRate(1), Rate.ToRate(15));

            var configurations = new SystemConfiguration[3];

            configurations[SystemType.Aris1800.IntegralValue] =
                new SystemConfiguration(SystemType.Aris1800)
                {
                    AvailablePingModes = new[] { PingMode.PingMode1, PingMode.PingMode3 },
                    DefaultPingMode = PingMode.PingMode3,
                    SampleCountPreferredLimits = new ValueRange<int>(1250, sampleCountDeviceLimits.Maximum),
                    ReceiverGainLimits = commonReceiverGainLimits,
                    FrameRateLimits = commonFrameRateLimits,
                    WindowStartLimits = RangeOfMeters(0.7, 25.0),
                    WindowEndLimits = RangeOfMeters(1.3, 50.0),

                    RawConfiguration = new SystemConfigurationRaw
                    {
                        SampleStartDelayLimits = RangeOfDuration(930, 36_000),
                        SamplePeriodLimits = RangeOfDuration(4, 20),
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

                    SmallPeriodAdjustmentFactor = 1.08,
                    LargePeriodAdjustmentFactor = 1.03,

                    MakeDefaultSettings = CreateSettingsBuilder(MakeDefaultSettings1800),

                    FrequencyHigh = Rate.ToRate(1_800_000),
                    FrequencyLow = Rate.ToRate(1_100_000),
                };

            configurations[SystemType.Aris3000.IntegralValue] =
                new SystemConfiguration(SystemType.Aris3000)
                {
                    AvailablePingModes = new[] { PingMode.PingMode6, PingMode.PingMode9 },
                    DefaultPingMode = PingMode.PingMode9,
                    SampleCountPreferredLimits = new ValueRange<int>(800, sampleCountDeviceLimits.Maximum),
                    ReceiverGainLimits = commonReceiverGainLimits,
                    FrameRateLimits = commonFrameRateLimits,
                    WindowStartLimits = RangeOfMeters(0.7, 12.0),
                    WindowEndLimits = RangeOfMeters(1.3, 20.0),

                    RawConfiguration = new SystemConfigurationRaw
                    {
                        SampleStartDelayLimits = RangeOfDuration(930, 18_000),
                        SamplePeriodLimits = RangeOfDuration(4, 12),
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

                    SmallPeriodAdjustmentFactor = 1.08,
                    LargePeriodAdjustmentFactor = 1.03,

                    MakeDefaultSettings = CreateSettingsBuilder(MakeDefaultSettings3000),

                    FrequencyHigh = Rate.ToRate(3_000_000),
                    FrequencyLow = Rate.ToRate(1_800_000),
                };

            configurations[SystemType.Aris1200.IntegralValue] =
                new SystemConfiguration(SystemType.Aris1200)
                {
                    AvailablePingModes = new[] { PingMode.PingMode1 },
                    DefaultPingMode = PingMode.PingMode1,
                    SampleCountPreferredLimits = new ValueRange<int>(1750, sampleCountDeviceLimits.Maximum),
                    ReceiverGainLimits = commonReceiverGainLimits,
                    FrameRateLimits = commonFrameRateLimits,
                    WindowStartLimits = RangeOfMeters(0.7, 40.0),
                    WindowEndLimits = RangeOfMeters(1.3, 100.0),

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

                    SmallPeriodAdjustmentFactor = 1.02,
                    LargePeriodAdjustmentFactor = 1.02, // large/small are the same for the 1200

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
