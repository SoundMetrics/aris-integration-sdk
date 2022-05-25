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

        public ValueRange<int> SampleCountRange { get; internal set; }

        public ValueRange<int> ReceiverGainRange { get; internal set; }

        public ValueRange<Rate> FrameRateRange { get; internal set; }

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
            => new ValueRange<Distance>(WindowStartRange.Minimum, WindowEndRange.Maximum);

        public ValueRange<Distance> WindowStartRange { get; internal set; }

        public ValueRange<Distance> WindowEndRange { get; internal set; }

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

        internal AcousticSettingsRaw GetDefaultSettings(ObservedConditions observedConditions)
            => MakeDefaultSettings(observedConditions);

        private Func<ObservedConditions, AcousticSettingsRaw> MakeDefaultSettings { get; set; }

        private static ValueRange<FineDuration> RangeOfDuration(double a, double b)
            => new ValueRange<FineDuration>(
                    FineDuration.FromMicroseconds(a),
                    FineDuration.FromMicroseconds(b));

        private static ValueRange<Distance> RangeOfMeters(double a, double b)
            => new ValueRange<Distance>((Distance)a, (Distance)b);

        private static readonly SystemConfiguration[] configurations = InitializeConfigurations();
    }
}
