﻿// Copyright (c) 2010-2021 Sound Metrics Corp.

using SoundMetrics.Aris.Core.Raw;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundMetrics.Aris.Core
{
    public static class SystemConfigurationExtensions
    {
        public static SystemConfiguration GetConfiguration(this SystemType systemType)
            => SystemConfiguration.GetConfiguration(systemType);
    }


    public sealed partial class SystemConfiguration
    {
        public static SystemConfiguration GetConfiguration(SystemType systemType)
        {
            return configurations[systemType.IntegralValue];
        }

        internal SystemConfiguration(SystemType systemType)
        {
            SystemType = systemType;
        }

        public SystemType SystemType { get; }

        public IReadOnlyCollection<PingMode> AvailablePingModes { get; internal set; }

        public bool IsValidPingMode(PingMode pingMode) => AvailablePingModes.Contains(pingMode);

        public PingMode DefaultPingMode { get; internal set; }

        public bool SupportsTelephotoLens { get; internal set; }

#pragma warning disable CA1822 // does not access instance data and can be marked as static
        // May eventually use instance data; don't mess up APIs just for warnings

        public InclusiveValueRange<int> SampleCountDeviceLimits => sampleCountDeviceLimits;

        internal InclusiveValueRange<int> PulseWidthDeviceLimits => pulseWidthDeviceLimits;

        internal InclusiveValueRange<int> SampleStartDelayDeviceLimits => sampleStartDelayDeviceLimits;

        internal InclusiveValueRange<int> FocusPositionDeviceLimits => focusPositionDeviceLimits;

        internal InclusiveValueRange<int> CyclePeriodDeviceLimits => cyclePeriodDeviceLimits;

#pragma warning restore CA1822

        public InclusiveValueRange<int> ReceiverGainLimits { get; internal set; }

        public InclusiveValueRange<Rate> FrameRateLimits { get; internal set; }

        /// <summary>
        /// Calculate the crossover distance given water temperature
        /// and salinity. If Window End is greater than the calculated value
        /// the lower frequency should be used.
        /// </summary>
        /// <param name="temperature">Water temperature</param>
        /// <param name="salinity">Water salinity</param>
        /// <returns>The crossover distance given current water temperature
        /// and salinity.</returns>
        internal Distance CalculateFrequencyCrossoverDistance(
            Temperature temperature,
            Salinity salinity)
            => AcousticSettingsAuto.CalculateFrequencyCrossoverDistance(
                    SystemType, temperature, salinity);

        /// <summary>
        /// Calculate the prefered frequency given water tempearture,
        /// salinity, and the window end.
        /// </summary>
        /// <param name="temperature">Water temperature</param>
        /// <param name="salinity">Water salinity</param>
        /// <param name="windowEnd">
        /// The window end; note that calculating this
        /// is dependent on conditions as well.
        /// </param>
        /// <returns>The prefered frequency.</returns>
        public Frequency CalculateBestFrequency(
            Temperature temperature,
            Salinity salinity,
            Distance windowEnd)
        {
            var crossover = CalculateFrequencyCrossoverDistance(temperature, salinity);
            return windowEnd > crossover ? Frequency.Low : Frequency.High;
        }

        public Frequency SelectFrequency(
            Temperature temperature,
            Salinity salinity,
            Distance windowEnd,
            bool useAutoFrequency,
            Frequency fallbackValue)
            => useAutoFrequency
                ? CalculateBestFrequency(temperature, salinity, windowEnd)
                : fallbackValue;

        public InclusiveValueRange<Distance> WindowLimits { get; internal set; }

        public static bool TryGetSampleGeometry(in FrameHeader frameHeader, out SampleGeometry sampleGeometry)
        {
            if (PingMode.TryGet((int)frameHeader.PingMode, out var pingMode))
            {
                var beamCount = pingMode.BeamCount;
                var totalSampleCount = beamCount * (int)frameHeader.SamplesPerBeam;

                sampleGeometry =
                    new SampleGeometry(
                        beamCount: beamCount,
                        sampleCount: (int)frameHeader.SamplesPerBeam,
                        totalSampleCount: totalSampleCount,
                        pingsPerFrame: pingMode.PingsPerFrame);
                return true;
            }
            else
            {
                sampleGeometry = default;
                return false;
            }
        }

        public SystemConfigurationRaw RawConfiguration { get; private set; }

        public Rate FrequencyLow { get; private set; }
        public Rate FrequencyHigh { get; private set; }

        public Rate AsRate(Frequency frequency)
            => frequency == Frequency.High ? FrequencyHigh : FrequencyLow;

        public AcousticSettingsRaw GetDefaultSettings(
            ObservedConditions observedConditions,
            Salinity salinity)
            => MakeDefaultSettings(observedConditions, salinity);

        private delegate AcousticSettingsRaw
            MakeDefaultSettingsFn(ObservedConditions observedConditions, Salinity salinity);

        private MakeDefaultSettingsFn MakeDefaultSettings { get; set; }

        private static InclusiveValueRange<FineDuration> RangeOfDuration(double a, double b)
            => new InclusiveValueRange<FineDuration>((FineDuration)a, (FineDuration)b);

        private static InclusiveValueRange<Distance> RangeOfMeters(double a, double b)
            => new InclusiveValueRange<Distance>((Distance)a, (Distance)b);

        // These must be initialized before `configurations`.
        private static readonly InclusiveValueRange<int> sampleCountDeviceLimits
            = new InclusiveValueRange<int>(200, 4000);
        private static readonly InclusiveValueRange<int> pulseWidthDeviceLimits
            = new InclusiveValueRange<int>(4, 80);
        private static readonly InclusiveValueRange<int> sampleStartDelayDeviceLimits
            = new InclusiveValueRange<int>(930, 60000);
        private static readonly InclusiveValueRange<int> focusPositionDeviceLimits
            = new InclusiveValueRange<int>(0, 1000);
        private static readonly InclusiveValueRange<int> cyclePeriodDeviceLimits
            = new InclusiveValueRange<int>(1802, 150000);

        private static readonly SystemConfiguration[] configurations = InitializeConfigurations();
    }
}
