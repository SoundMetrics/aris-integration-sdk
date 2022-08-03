// Copyright (c) 2010-2021 Sound Metrics Corp.

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

#pragma warning disable CA1822 // does not access instance data and can be marked as static
        // May eventually use instance data; don't mess up APIs just for warnings

        public ValueRange<int> SampleCountDeviceLimits => sampleCountDeviceLimits;

        internal ValueRange<int> PulseWidthDeviceLimits => pulseWidthDeviceLimits;

        internal ValueRange<int> SampleStartDelayDeviceLimits => sampleStartDelayDeviceLimits;

        internal ValueRange<int> SamplePeriodDeviceLimits => samplePeriodDeviceLimits;

        internal ValueRange<int> FocusPositionDeviceLimits => focusPositionDeviceLimits;

        internal ValueRange<int> CyclePeriodDeviceLimits => cyclePeriodDeviceLimits;

#pragma warning restore CA1822

        /// <summary>
        /// The preferred limits for sample count as used in SMC software.
        /// </summary>
        public ValueRange<int> SampleCountPreferredLimits { get; internal set; }

        public ValueRange<int> ReceiverGainLimits { get; internal set; }

        public ValueRange<Rate> FrameRateLimits { get; internal set; }

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
            => AcousticSettingsRaw_Aux.CalculateFrequencyCrossoverDistance(
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

        public ValueRange<Distance> UsefulImagingRange
            => new ValueRange<Distance>(WindowStartLimits.Minimum, WindowEndLimits.Maximum);

        public ValueRange<Distance> WindowStartLimits { get; internal set; }

        public ValueRange<Distance> WindowEndLimits { get; internal set; }

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

        internal double SmallPeriodAdjustmentFactor { get; set; }
        internal double LargePeriodAdjustmentFactor { get; set; }

        public AcousticSettingsRaw GetDefaultSettings(ObservedConditions observedConditions)
            => MakeDefaultSettings(observedConditions);

        private Func<ObservedConditions, AcousticSettingsRaw> MakeDefaultSettings { get; set; }

        private static ValueRange<FineDuration> RangeOfDuration(double a, double b)
            => new ValueRange<FineDuration>(
                    FineDuration.FromMicroseconds(a),
                    FineDuration.FromMicroseconds(b));

        private static ValueRange<Distance> RangeOfMeters(double a, double b)
            => new ValueRange<Distance>((Distance)a, (Distance)b);

        // These must be initialized before `configurations`.
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

        private static readonly SystemConfiguration[] configurations = InitializeConfigurations();
    }
}
