// Copyright (c) 2010-2022 Sound Metrics Corp.

using SoundMetrics.Aris.Core.ApprovalTests;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace SoundMetrics.Aris.Core.Raw
{
#pragma warning disable CA1051 // Do not declare visible instance fields


    /// <summary>
    /// This type is ported over from some legacy ARIS Integration SDK work,
    /// and will continue in new ARIS Integration SDK work.
    /// </summary>
    [DataContract]
    public sealed partial class AcousticSettingsRaw
        : IEquatable<AcousticSettingsRaw>, ApprovalTests.IPrettyPrintable
    {
        public AcousticSettingsRaw(
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
            in FocusDistance focusDistance,
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
            FocusDistance = focusDistance;
            AntiAliasing = antiAliasing;
            InterpacketDelay = interpacketDelay;
            Salinity = salinity;

            MaximumFrameRate = MaxFrameRate.CalculateMaximumFrameRate(this);

            CheckInvariants(this);
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

        [DataMember(Name = "FocusDistance", EmitDefaultValue = false)]
        private Distance _obsoleteFocusDistance;

        [DataMember(Name = "FocusDistanceV2")]
        private FocusDistance _focusDistanceV2;

        // These are not directly an acoustic setting, but part of the package.
        public FocusDistance FocusDistance
        {
            get => _focusDistanceV2;
            private set => _focusDistanceV2 = value;
        }

        [DataMember]
        public FineDuration AntiAliasing { get; private set; }
        [DataMember]
        public InterpacketDelaySettings InterpacketDelay { get; private set; }
        [DataMember]
        public Rate MaximumFrameRate { get; private set; }

        public static Rate MinimumFrameRate => Rate.OneHertz;

        [DataMember]
        public Salinity Salinity { get; private set; }

        public FineDuration CyclePeriod
        {
            get
            {
                var _ = MaxFrameRate.CalculateMaximumFrameRate(this, out var cyclePeriod);
                return cyclePeriod;
            }
        }

        private Distance WindowStart(ObservedConditions observedConditions)
            => SampleStartDelay * observedConditions.SpeedOfSound(Salinity) / 2;

        private Distance WindowEnd(ObservedConditions observedConditions)
            => WindowStart(observedConditions) + WindowLength(observedConditions);

        private Distance WindowLength(ObservedConditions observedConditions)
            => SampleCount * SamplePeriod * observedConditions.SpeedOfSound(Salinity) / 2;

        /// <summary>
        /// Calculates the bounds of the imaging window.
        /// </summary>
        /// <param name="observedConditions">
        /// Observations necessary for correct calculations.
        /// </param>
        /// <returns>The bounds of the imaging window.</returns>
        public WindowBounds WindowBounds(ObservedConditions observedConditions)
            => new WindowBounds(WindowStart(observedConditions), WindowEnd(observedConditions));

        public Distance SampleResolution(ObservedConditions observedConditions)
            => observedConditions.ConvertSamplePeriodToResolution(SamplePeriod, Salinity);

        public override bool Equals(object? obj) => Equals(obj as AcousticSettingsRaw);

        public bool Equals(AcousticSettingsRaw? other)
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
                && this.FocusDistance == other.FocusDistance
                && this.AntiAliasing == other.AntiAliasing
                && this.InterpacketDelay == other.InterpacketDelay
                && this.Salinity == other.Salinity;
        }

        public static bool operator ==(AcousticSettingsRaw a, AcousticSettingsRaw b)
        {
            if (a is null)
            {
                if (b is null)
                {
                    return true;
                }
                else
                {
                    // Only a is null
                    return false;
                }
            }

            return a.Equals(b); // Instance method Equals() can handle null arg.
        }

        public static bool operator !=(AcousticSettingsRaw a, AcousticSettingsRaw b)
            => !(a == b);

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
                ^ FocusDistance.GetHashCode()
                ^ AntiAliasing.GetHashCode()
                ^ InterpacketDelay.GetHashCode()
                ^ Salinity.GetHashCode();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            // Update old value type to new and clear the old.
            if (_obsoleteFocusDistance != default)
            {
                FocusDistance = _obsoleteFocusDistance;
                _obsoleteFocusDistance = default;
                Trace.TraceInformation("Converted old-format focus distance");
            }

            CheckInvariants(this);
        }

        public override string ToString()
            => $"systemType={SystemType}; frameRate={FrameRate}; sampleCount={SampleCount}; sampleStartDelay={SampleStartDelay}; "
                + $"cyclePeriod={CyclePeriod}; samplePeriod={SamplePeriod}; pulseWidth={PulseWidth}; "
                + $"pingMode={PingMode}; enableTransmit={EnableTransmit}; frequency={Frequency}; enable150Volts={Enable150Volts}; "
                + $"receiverGain={ReceiverGain}; FocusDistance={FocusDistance}; AntiAliasing={AntiAliasing}; "
                + $"InterpacketDelay={InterpacketDelay}; Salinity={Salinity}";

        // Pass in the instance. At present, we don't have use of
        // init-only properties due to targeting .NET Standard 2.0.
        private static void CheckInvariants(AcousticSettingsRaw settings)
        {
            var sysCfg = settings.SystemType.GetConfiguration();
            var rawCfg = sysCfg.RawConfiguration;

            ValidateRange(
                nameof(settings.SamplePeriod),
                settings.SamplePeriod,
                rawCfg.SamplePeriodLimits);
        }

        private static void ValidateRange<TValue>(
            string valueName,
            in TValue value,
            in InclusiveValueRange<TValue> valueRange)
            where TValue : struct, IComparable
        {
            if (!valueRange.Contains(value))
            {
                var errorMessage =
                    BuildRangeValidationErrorMessage(
                        valueName,
                        value,
                        valueRange);
                throw new ArgumentOutOfRangeException(errorMessage);
            }
        }

        private static string BuildRangeValidationErrorMessage<TValue>(
            string valueName,
            in TValue value,
            in InclusiveValueRange<TValue> valueRange)
            where TValue : struct, IComparable
            =>
            $"Value '{valueName}' is [{value}]; this is not in range [{valueRange}]";

        PrettyPrintHelper IPrettyPrintable.PrettyPrint(
            PrettyPrintHelper helper,
            string label)
        {
            helper.PrintHeading($"{label}: {nameof(AcousticSettingsRaw)}");

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
                helper.PrintValue(nameof(FocusDistance), FocusDistance);
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
