// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Collections.Generic;
using System.Text;

namespace SoundMetrics.Aris.Core.Raw
{
    /// <summary>
    /// This type is ported over from some legacy ARIS Integration SDK work,
    /// and will continue in new ARIS Integration SDK work.
    /// </summary>
    public sealed partial class AcousticSettingsRaw
    {
        public SystemType SystemType { get; private set; }
        public Rate FrameRate { get; private set; }
        public int SamplesPerBeam { get; private set; }
        public FineDuration SampleStartDelay { get; private set; }
        public FineDuration CyclePeriod { get; private set; }
        public FineDuration SamplePeriod { get; private set; }
        public FineDuration PulseWidth { get; private set; }
        public PingMode PingMode { get; private set; }
        public bool EnableTransmit { get; private set; }
        public FrequencySelection Frequency { get; private set; }
        public bool Enable150Volts { get; private set; }
        public int ReceiverGain { get; private set; }

        // There are not directly an acoustic setting, but part of the package.
        public Distance FocusPosition { get; private set; }
        public FineDuration AntiAliasing { get; private set; }
        public InterpacketDelaySettings InterpacketDelay { get; private set; }

        /// <summary>
        /// Environmental status when the settings were created.
        /// </summary>
        public EnvironmentalConditions SonarEnvironment { get; private set; }

        public Distance WindowStart => CalculateWindowStart(SampleStartDelay, SonarEnvironment.SpeedOfSound);
        public Distance WindowEnd => WindowStart + WindowLength;
        public Distance WindowLength => CalculateWindowLength(SamplesPerBeam, SamplePeriod, SonarEnvironment.SpeedOfSound);

        public static Distance CalculateWindowStart(FineDuration sampleStartDelay, Velocity speedOfSound)
            => sampleStartDelay * speedOfSound / 2;
        public static Distance CalculateWindowLength(int samplesPerBeam, FineDuration samplePeriod, Velocity speedOfSound)
            => samplesPerBeam * samplePeriod * speedOfSound / 2;

        public static FineDuration CalculateSampleStartDelay(Distance windowStart, Velocity speedOfSound)
            => 2 * (windowStart / speedOfSound);

        public AcousticSettingsRaw(
            SystemType systemType,
            Rate frameRate,
            int samplesPerBeam,
            FineDuration sampleStartDelay,
            FineDuration cyclePeriod,
            FineDuration samplePeriod,
            FineDuration pulseWidth,
            PingMode pingMode,
            bool enableTransmit,
            FrequencySelection frequency,
            bool enable150Volts,
            int receiverGain,
            Distance focusPosition,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay,
            EnvironmentalConditions sonarEnvironment)
        {
            SystemType = systemType;
            FrameRate = frameRate;
            SamplesPerBeam = samplesPerBeam;
            SampleStartDelay = sampleStartDelay;
            CyclePeriod = cyclePeriod;
            SamplePeriod = samplePeriod;
            PulseWidth = pulseWidth;
            PingMode = pingMode;
            EnableTransmit = enableTransmit;
            Frequency = frequency;
            Enable150Volts = enable150Volts;
            ReceiverGain = receiverGain;
            FocusPosition = focusPosition;
            AntiAliasing = antiAliasing;
            InterpacketDelay = interpacketDelay;
            SonarEnvironment = sonarEnvironment;
        }

        public override string ToString()
            => $"systemType={SystemType}; frameRate={FrameRate}; sampleCount={SamplesPerBeam}; sampleStartDelay={SampleStartDelay}; "
                + $"cyclePeriod={CyclePeriod}; samplePeriod={SamplePeriod}; pulseWidth={PulseWidth}; "
                + $"pingMode={PingMode}; enableTransmit={EnableTransmit}; frequency={Frequency}; enable150Volts={Enable150Volts}; "
                + $"receiverGain={ReceiverGain}; FocusPosition={FocusPosition}; AntiAliasing={AntiAliasing}; "
                + $"InterpacketDelay={InterpacketDelay}; SonarEnvironment={SonarEnvironment}; "
                + $"CALCULATED[WindowStart={WindowStart}; WindowEnd={WindowEnd}; WindowLength={WindowLength}]";
    }
}
