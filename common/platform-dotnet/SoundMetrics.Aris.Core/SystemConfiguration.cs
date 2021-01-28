// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Core
{
    using static SystemConfigurationRaw;

    public sealed partial class SystemConfiguration
    {
        public static SystemConfiguration GetConfiguration(SystemType systemType)
            =>
                configurations[systemType.IntegralValue];

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

        public ValueRange<Distance> GetUsefulImagingRange(Frequency frequency)
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
                        samplesPerBeam: (int)frameHeader.SamplesPerBeam,
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

        public SystemConfigurationRaw RawConfiguration { get; set; }

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
