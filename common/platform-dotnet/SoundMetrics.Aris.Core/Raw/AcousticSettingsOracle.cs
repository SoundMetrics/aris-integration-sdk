// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsConstraints;

    public struct AcousticSettingsRequest : IEquatable<AcousticSettingsRequest>
    {
        internal AcousticSettingsRequest(
            AcousticSettingsRaw requested,
            AcousticSettingsRaw allowed)
        {
            if (requested is null) throw new ArgumentNullException(nameof(requested));
            if (allowed is null) throw new ArgumentNullException(nameof(allowed));

            Requested = requested;
            Allowed = allowed;
        }

        public AcousticSettingsRaw Requested { get; private set; }
        public AcousticSettingsRaw Allowed { get; private set; }

        public override bool Equals(object obj)
            => obj is AcousticSettingsRequest other && this.Equals(other);

        public bool Equals(AcousticSettingsRequest other)
            => !(Requested is null)
                && Requested.Equals(other.Requested)
                && !(Allowed is null)
                && Allowed.Equals(other.Allowed);

        public override int GetHashCode()
            => Requested.GetHashCode() ^ Allowed.GetHashCode();

        public static bool operator ==(AcousticSettingsRequest left, AcousticSettingsRequest right)
            => left.Equals(right);

        public static bool operator !=(AcousticSettingsRequest left, AcousticSettingsRequest right)
            => !left.Equals(right);
    }

    public static class AcousticSettingsOracle
    {
        /// <summary>
        /// Initializes a new instance of acoustic settings.
        /// </summary>
        public static AcousticSettingsRequest Initialize(
            SystemType systemType,
            Rate frameRate,
            int sampleCount,
            FineDuration sampleStartDelay,
            FineDuration cyclePeriod,
            FineDuration samplePeriod,
            FineDuration pulseWidth,
            PingMode pingMode,
            bool enableTransmit,
            Frequency frequency,
            bool enable150Volts,
            int receiverGain,
            Distance focusPosition,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay,
            EnvironmentalContext sonarEnvironment)
        {
            // This method is the only public route to a new instance of AcousticSettingsRaw.
            // This promotes strong control over the values in AcousticSettingsRaw.

            var requested = new AcousticSettingsRaw(
                systemType,
                frameRate,
                sampleCount,
                sampleStartDelay,
                cyclePeriod,
                samplePeriod,
                pulseWidth,
                pingMode,
                enableTransmit,
                frequency,
                enable150Volts,
                receiverGain,
                focusPosition,
                antiAliasing,
                interpacketDelay,
                sonarEnvironment);
            var allowed = ApplyAllConstraints(requested);

            return new AcousticSettingsRequest(requested, allowed);
        }

        private static AcousticSettingsRaw ApplyAllConstraints(AcousticSettingsRaw requested)
        {
            var sysCfg = SystemConfiguration.GetConfiguration(requested.SystemType);
            var settings = requested;
            // ### TODO much else
            settings = UpdateFrameRate(
                settings,
                ConstrainFrameRate(
                    settings.FrameRate,
                    sysCfg,
                    settings.PingMode,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.AntiAliasing,
                    settings.InterpacketDelay));
            return settings;
        }

        public static AcousticSettingsRequest SetFrameRate(
            this AcousticSettingsRaw settings,
            Rate requestedFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);
            var allowedFrameRate =
                ConstrainFrameRate(
                    requestedFrameRate,
                    sysCfg,
                    settings.PingMode,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.AntiAliasing,
                    settings.InterpacketDelay);

            return new AcousticSettingsRequest(
                requested: UpdateFrameRate(settings, requestedFrameRate),
                allowed: UpdateFrameRate(settings, allowedFrameRate));
        }

        public static AcousticSettingsRequest SetMaxFrameRate(
            this AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            return settings.SetFrameRate(settings.MaximumFrameRate);
        }

        private static AcousticSettingsRaw UpdateFrameRate(
            AcousticSettingsRaw settings, Rate newFrameRate)
        {
            return settings.FrameRate.Equals(newFrameRate)
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    newFrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.CyclePeriod,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusPosition,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.SonarEnvironment);
        }

        //private static Rate ConstrainFrameRate(
        //    Rate requestedFrameRate,
        //    AcousticSettingsRaw settings)
        //{
        //    var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);
        //    var min = sysCfg.FrameRateRange.Minimum;
        //    var max = MaxFrameRate.DetermineMaximumFrameRate(
        //                settings.SystemType,
        //                settings.PingMode,
        //                settings.SampleCount,
        //                settings.SampleStartDelay,
        //                settings.SamplePeriod,
        //                settings.AntiAliasing,
        //                settings.InterpacketDelay);

        //    var allowedFrameRate = Rate.Max(min, Rate.Min(max, requestedFrameRate));
        //    return allowedFrameRate;
        //}

        public static AcousticSettingsRequest SetFocusPosition(
            this AcousticSettingsRaw settings,
            Distance newFocusPosition)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            var newSettings =
                new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.CyclePeriod,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    newFocusPosition,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.SonarEnvironment);
            return new AcousticSettingsRequest(
                requested: newSettings,
                allowed: newSettings);
        }

        public static AcousticSettingsRequest SetAutomatedFocus(this AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            return SetFocusPosition(settings, settings.WindowMidPoint);
        }

        public static AcousticSettingsRequest SetInterpacketDelay(
            this AcousticSettingsRaw settings,
            InterpacketDelaySettings newInterpacketDelay,
            bool useMaxFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            var newSettings =
                new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.CyclePeriod,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusPosition,
                    settings.AntiAliasing,
                    newInterpacketDelay,
                    settings.SonarEnvironment);
            var result =
                useMaxFrameRate ? newSettings.SetMaxFrameRate().Allowed : newSettings;
            return new AcousticSettingsRequest(result, result);
        }

        public static AcousticSettingsRequest SetReceiverGain(
            this AcousticSettingsRaw settings,
            float gain)
        {
            throw new NotImplementedException();
        }

        //public static AcousticSettingsRequest SetTransmitEnable(
        //    this AcousticSettingsRaw settings,
        //    bool enable)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRequest Set150VoltsEnable(
        //    this AcousticSettingsRaw settings,
        //    bool enable)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRequest SetSampleCount(
        //    this AcousticSettingsRaw settings,
        //    int sampleCount)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRequest SetFrequency(
        //    this AcousticSettingsRaw settings,
        //    Frequency frequency)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRequest SetPulseWidth(
        //    this AcousticSettingsRaw settings,
        //    FineDuration pulseWidth)
        //{
        //    throw new NotImplementedException();

        //    /*
        //            var systemType = SoundMetrics.Aris.Core.SystemType.GetFromIntegralValue((int)connection.SystemType);
        //            var pulseWidth = FineDuration.FromMicroseconds(pulseWidthUsec);
        //            var frequency = (SoundMetrics.Aris.Core.Frequency)settings.frequency;
        //            var frameRate = (Rate)settings.frameRate;

        //            safePulseWidth =
        //                (uint)MakeSafePulseWidth(
        //                        systemType,
        //                        pulseWidth,
        //                        frequency,
        //                        frameRate)
        //                        .Floor
        //                        .TotalMicroseconds;
        //            settings.pulseWidth = safePulseWidth;
        //     */
        //}
    }
}
