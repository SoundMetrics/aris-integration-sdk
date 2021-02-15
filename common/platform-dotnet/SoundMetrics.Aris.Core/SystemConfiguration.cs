// Copyright (c) 2010-2021 Sound Metrics Corp.

using SoundMetrics.Aris.Core.Raw;
using System;
using System.Collections.Generic;

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

        public IReadOnlyCollection<PingMode> PingModes { get; internal set; }

        public PingMode DefaultPingMode { get; internal set; }

        public ValueRange<int> SampleCountRange { get; internal set; }

        public ValueRange<int> ReceiverGainRange { get; internal set; }

        public ValueRange<Rate> FrameRateRange { get; internal set; }

        /// <summary>
        /// The distance at which the low or high frequency is chosen.
        /// The lower frequency is used for longer distances.
        /// </summary>
        public Distance FrequencyCrossover { get; internal set; }

        public ValueRange<Distance> UsefulHighFrequencyImagingRange { get; internal set; }

        public ValueRange<Distance> UsefulLowFrequencyImagingRange { get; internal set; }

        public ValueRange<Distance> CombinedUsefulImagingRange
            => UsefulHighFrequencyImagingRange.Union(UsefulLowFrequencyImagingRange);

        public ValueRange<Distance> UsefulImagingRange
            => UsefulHighFrequencyImagingRange.Union(UsefulLowFrequencyImagingRange);

        public ValueRange<Distance> GetUsefulImagingRangeFor(Frequency frequency)
        {
            switch (frequency)
            {
                case Frequency.Low:
                    return UsefulLowFrequencyImagingRange;
                case Frequency.High:
                    return UsefulHighFrequencyImagingRange;
                default:
                    throw new ArgumentException($"Unexpected value for {nameof(frequency)}: {frequency}");
            }
        }

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

        public Rate GetFrequency(Frequency frequency)
            => frequency == Frequency.High ? FrequencyHigh : FrequencyLow;

        internal double SmallPeriodAdjustmentFactor { get; set; }
        internal double LargePeriodAdjustmentFactor { get; set; }

        internal AcousticSettingsRaw GetDefaultSettings(EnvironmentalContext sonarEnvironment)
            => MakeDefaultSettings(sonarEnvironment);

        private Func<EnvironmentalContext, AcousticSettingsRaw> MakeDefaultSettings { get; set; }

        private static ValueRange<FineDuration> RangeOfDuration(double a, double b)
            => new ValueRange<FineDuration>(
                    FineDuration.FromMicroseconds(a),
                    FineDuration.FromMicroseconds(b));

        private static ValueRange<Distance> RangeOfMeters(double a, double b)
            => new ValueRange<Distance>(
                    Distance.FromMeters(a),
                    Distance.FromMeters(b));

        private static readonly SystemConfiguration[] configurations = InitializeConfigurations();
    }
}
