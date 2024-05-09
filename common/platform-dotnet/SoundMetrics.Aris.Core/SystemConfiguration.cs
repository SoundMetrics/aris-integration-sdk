// Copyright (c) 2010-2021 Sound Metrics Corp.

using SoundMetrics.Aris.Core.Raw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            AvailablePingModes = new List<PingMode>();
            RawConfiguration = new SystemConfigurationRaw();
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

        public float DefaultReceiverGain { get; init; }

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

        public WindowBounds DefaultWindow { get; init; }

        public AcousticSettingsRaw GetDefaultSettings(
            ObservedConditions observedConditions,
            Salinity salinity)
        {
            var window = DefaultWindow;
            var sspd = observedConditions.SpeedOfSound(salinity);
            var pingMode = DefaultPingMode;
            var antiAliasing = FineDuration.Zero;
            var interpacketDelay = InterpacketDelaySettings.Off;

            var samplePeriod =
                AcousticSettingsAuto.CalculateAutoSamplePeriod(
                    SystemType,
                    observedConditions.WaterTemp,
                    window.WindowEnd);

            var sampleCount =
                AdjustWindowTerminusLevel2.CalculateNominalSampleCount(
                    samplePeriod,
                    window.WindowStart,
                    window.WindowEnd,
                    sspd,
                    out var correctedWindowEnd);

            // NOTE: correcting the window end to fit sample count and sample period.
            window = new WindowBounds(window.WindowStart, correctedWindowEnd);

            var pulseWidth =
                AcousticSettingsAuto.CalculateAutoPulseWidth(
                    SystemType,
                    observedConditions.WaterTemp,
                    salinity,
                    window.WindowEnd);
            var frequency =
                AcousticSettingsAuto.CalculateFrequencyPerWindowEnd(
                    SystemType,
                    observedConditions.WaterTemp,
                    salinity,
                    window.WindowEnd);


            var sampleStartDelay =
                BasicCalculations.CalculateSampleStartDelay(
                    window.WindowStart,
                    sspd);

            var maxFrameRate =
                MaxFrameRate.CalculateMaximumFrameRate(
                    this,
                    pingMode,
                    sampleCount,
                    sampleStartDelay,
                    samplePeriod,
                    antiAliasing,
                    interpacketDelay);

            // -------------------------------------------------------

            var receiverGain = DefaultReceiverGain;
            var enableTransmit = true;
            var enable150Volts = true;
            var fakeFocusDistance = (Distance)10;


            return new AcousticSettingsRaw(
                    SystemType, maxFrameRate, sampleCount, sampleStartDelay, samplePeriod,
                    pulseWidth, pingMode, enableTransmit, frequency, enable150Volts, receiverGain,
                    fakeFocusDistance, antiAliasing, interpacketDelay, salinity)
                .WithAutomaticSettings(
                    observedConditions,
                    AutomaticAcousticSettings.FocusDistance);
        }

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
