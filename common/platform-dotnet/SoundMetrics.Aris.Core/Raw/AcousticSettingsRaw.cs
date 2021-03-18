// Copyright (c) 2010-2021 Sound Metrics Corp.

using SoundMetrics.Aris.Core.ApprovalTests;
using System;
using System.Runtime.Serialization;

namespace SoundMetrics.Aris.Core.Raw
{
#pragma warning disable CA1051 // Do not declare visible instance fields

    using static FineDuration;

    /// <summary>
    /// This type is ported over from some legacy ARIS Integration SDK work,
    /// and will continue in new ARIS Integration SDK work.
    /// </summary>
    [DataContract]
    public sealed partial class AcousticSettingsRaw
        : IEquatable<AcousticSettingsRaw>, ApprovalTests.IPrettyPrintable
    {
        internal AcousticSettingsRaw(
            SystemType systemType,
            Rate frameRate,
            int sampleCount,
            FineDuration sampleStartDelay,
            FineDuration samplePeriod,
            FineDuration pulseWidth,
            PingMode pingMode,
            bool enableTransmit,
            Frequency frequency,
            bool enable150Volts,
            float receiverGain,
            Distance focusPosition,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay,
            Salinity salinity)
        {
            SystemType = systemType;
            FrameRate = frameRate;
            SampleCount = sampleCount;
            SampleStartDelay = sampleStartDelay;
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
            Salinity = salinity;

            MaximumFrameRate = MaxFrameRate.DetermineMaximumFrameRate(this);
        }

        [DataMember]
        public SystemType SystemType { get; private set; }
        [DataMember]
        public Rate FrameRate { get; private set; }
        [DataMember]
        public int SampleCount { get; private set; }
        [DataMember]
        public FineDuration SampleStartDelay { get; private set; }
        [DataMember]
        public FineDuration SamplePeriod { get; private set; }
        [DataMember]
        public FineDuration PulseWidth { get; private set; }
        [DataMember]
        public PingMode PingMode { get; private set; }
        [DataMember]
        public bool EnableTransmit { get; private set; }
        [DataMember]
        public Frequency Frequency { get; private set; }
        [DataMember]
        public bool Enable150Volts { get; private set; }
        [DataMember]
        public float ReceiverGain { get; private set; }

        // There are not directly an acoustic setting, but part of the package.
        [DataMember]
        public Distance FocusPosition { get; private set; }
        [DataMember]
        public FineDuration AntiAliasing { get; private set; }
        [DataMember]
        public InterpacketDelaySettings InterpacketDelay { get; private set; }
        [DataMember]
        public Rate MaximumFrameRate { get; private set; }

#pragma warning disable CA1822 // does not access instance data and can be marked as static
        // Leave this as an instance property in case it ever needs to be an instance property
        // rather than class property.
        public Rate MinimumFrameRate => Rate.OneHertz;
#pragma warning restore CA1822 // does not access instance data and can be marked as static

        [DataMember]
        public Salinity Salinity { get; private set; }

        public FineDuration CyclePeriod
        {
            get
            {
                var _ = MaxFrameRate.DetermineMaximumFrameRate(this, out var cyclePeriod);
                return cyclePeriod;
            }
        }

        public Distance WindowStart(ObservedConditions observedConditions)
            => this.CalculateWindowStart(observedConditions, Salinity);
        public Distance WindowEnd(ObservedConditions observedConditions)
            => WindowStart(observedConditions) + WindowLength(observedConditions);
        public Distance WindowLength(ObservedConditions observedConditions)
            => this.CalculateWindowLength(observedConditions, Salinity);
        public Distance WindowMidPoint(ObservedConditions observedConditions)
            => WindowStart(observedConditions)+ (WindowLength(observedConditions) / 2);

        public override bool Equals(object obj) => Equals(obj as AcousticSettingsRaw);

        public bool Equals(AcousticSettingsRaw other)
        {
            if (other is null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return
                this.SystemType == other.SystemType
                && this.FrameRate == other.FrameRate
                && this.SampleCount == other.SampleCount
                && this.SampleStartDelay == other.SampleStartDelay
                && this.CyclePeriod == other.CyclePeriod
                && this.SamplePeriod == other.SamplePeriod
                && this.PulseWidth == other.PulseWidth
                && this.PingMode == other.PingMode
                && this.EnableTransmit == other.EnableTransmit
                && this.Frequency == other.Frequency
                && this.Enable150Volts == other.Enable150Volts
                && this.ReceiverGain == other.ReceiverGain
                && this.FocusPosition == other.FocusPosition
                && this.AntiAliasing == other.AntiAliasing
                && this.InterpacketDelay == other.InterpacketDelay
                && this.Salinity == other.Salinity;
        }

        public static bool operator ==(AcousticSettingsRaw a, AcousticSettingsRaw b)
            => !(a is null) && a.Equals(b);

        public static bool operator !=(AcousticSettingsRaw a, AcousticSettingsRaw b)
            => (a is null) || !a.Equals(b);

        public override int GetHashCode()
        {
            return
                base.GetHashCode()
                ^ SystemType.GetHashCode()
                ^ FrameRate.GetHashCode()
                ^ SampleCount.GetHashCode()
                ^ SampleStartDelay.GetHashCode()
                ^ CyclePeriod.GetHashCode()
                ^ SamplePeriod.GetHashCode()
                ^ PulseWidth.GetHashCode()
                ^ PingMode.GetHashCode()
                ^ EnableTransmit.GetHashCode()
                ^ Frequency.GetHashCode()
                ^ Enable150Volts.GetHashCode()
                ^ ReceiverGain.GetHashCode()
                ^ FocusPosition.GetHashCode()
                ^ AntiAliasing.GetHashCode()
                ^ InterpacketDelay.GetHashCode()
                ^ Salinity.GetHashCode();
        }

        public override string ToString()
            => $"systemType={SystemType}; frameRate={FrameRate}; sampleCount={SampleCount}; sampleStartDelay={SampleStartDelay}; "
                + $"cyclePeriod={CyclePeriod}; samplePeriod={SamplePeriod}; pulseWidth={PulseWidth}; "
                + $"pingMode={PingMode}; enableTransmit={EnableTransmit}; frequency={Frequency}; enable150Volts={Enable150Volts}; "
                + $"receiverGain={ReceiverGain}; FocusPosition={FocusPosition}; AntiAliasing={AntiAliasing}; "
                + $"InterpacketDelay={InterpacketDelay}; Salinity={Salinity}";

        PrettyPrintHelper IPrettyPrintable.PrettyPrint(PrettyPrintHelper helper)
        {
            helper.PrintHeading($"{nameof(AcousticSettingsRaw)}");

            using (var _ = helper.PushIndent())
            {

                helper.PrintValue(nameof(SystemType), SystemType);
                helper.PrintValue(nameof(FrameRate), FrameRate);
                helper.PrintValue(nameof(SampleCount), SampleCount);
                helper.PrintValue(nameof(SampleStartDelay), SampleStartDelay);
                helper.PrintValue(nameof(SamplePeriod), SamplePeriod);
                helper.PrintValue(nameof(PulseWidth), PulseWidth);
                helper.PrintValue(nameof(PingMode), PingMode);
                helper.PrintValue(nameof(EnableTransmit), EnableTransmit);
                helper.PrintValue(nameof(Frequency), Frequency);
                helper.PrintValue(nameof(Enable150Volts), Enable150Volts);
                helper.PrintValue(nameof(ReceiverGain), ReceiverGain);
                helper.PrintValue(nameof(FocusPosition), FocusPosition);
                helper.PrintValue(nameof(AntiAliasing), AntiAliasing);
                helper.PrintValue(nameof(InterpacketDelay), InterpacketDelay);
                helper.PrintValue(nameof(Salinity), Salinity);
                helper.PrintValue($"{nameof(CyclePeriod)} (calculated)", CyclePeriod);
            }

            return helper;
        }
    }

#pragma warning restore CA1051 // Do not declare visible instance fields
}
