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
        public int SampleCount { get; private set; }
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
        public FocusPosition FocusPosition { get; private set; }
        public FineDuration AntiAliasing { get; private set; }
        public InterpacketDelaySettings InterpacketDelay { get; private set; }

        /// <summary>
        /// Environmental status when the settings were created.
        /// </summary>
        public EnvironmentalContext SonarEnvironment { get; private set; }

        public Distance WindowStart => this.CalculateWindowStart();
        public Distance WindowEnd => WindowStart + WindowLength;
        public Distance WindowLength => this.CalculateWindowLength();

        internal AcousticSettingsRaw(
            SystemType systemType,
            Rate frameRate,
            int sampleCount,
            FineDuration sampleStartDelay,
            FineDuration cyclePeriod,
            FineDuration samplePeriod,
            FineDuration pulseWidth,
            PingMode pingMode,
            bool enableTransmit,
            FrequencySelection frequency,
            bool enable150Volts,
            int receiverGain,
            FocusPosition focusPosition,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay,
            EnvironmentalContext sonarEnvironment)
        {
            SystemType = systemType;
            FrameRate = frameRate;
            SampleCount = sampleCount;
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
            => $"systemType={SystemType}; frameRate={FrameRate}; sampleCount={SampleCount}; sampleStartDelay={SampleStartDelay}; "
                + $"cyclePeriod={CyclePeriod}; samplePeriod={SamplePeriod}; pulseWidth={PulseWidth}; "
                + $"pingMode={PingMode}; enableTransmit={EnableTransmit}; frequency={Frequency}; enable150Volts={Enable150Volts}; "
                + $"receiverGain={ReceiverGain}; FocusPosition={FocusPosition}; AntiAliasing={AntiAliasing}; "
                + $"InterpacketDelay={InterpacketDelay}; SonarEnvironment={SonarEnvironment}; "
                + $"CALCULATED[WindowStart={WindowStart}; WindowEnd={WindowEnd}; WindowLength={WindowLength}]";
    }
}
