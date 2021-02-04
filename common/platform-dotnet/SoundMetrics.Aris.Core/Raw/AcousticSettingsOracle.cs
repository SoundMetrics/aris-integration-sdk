// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsConstraints;

    public struct AcousticSettingsRequest
    {
        internal AcousticSettingsRequest(
            AcousticSettingsRaw requested,
            AcousticSettingsRaw allowed)
        {
            Requested = requested;
            Allowed = allowed;
        }

        public AcousticSettingsRaw Requested { get; private set; }
        public AcousticSettingsRaw Allowed { get; private set; }
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
            FocusPosition focusPosition,
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
            var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);
            var allowedFrameRate =
                ConstrainFrameRate(
                    settings.FrameRate,
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

        public static AcousticSettingsRequest SetSampleCount(int sampleCount)
            => throw new NotImplementedException();
    }
}
