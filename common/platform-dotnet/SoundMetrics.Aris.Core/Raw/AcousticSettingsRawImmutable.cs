using System;

namespace SoundMetrics.Aris.Core.Raw
{
    /// <summary>
    /// Support for immutable behavior before switching to a .NET Core-based distribution.
    /// </summary>
    public static class AcousticSettingsRawImmutable
    {
        public static AcousticSettingsRaw UpdateFrameRate(
            this AcousticSettingsRaw settings,
            Rate newFrameRate)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = settings.FrameRate == newFrameRate
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    newFrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            return result;
        }

        public static AcousticSettingsRaw WithTransmit(
            this AcousticSettingsRaw settings,
            bool enableTransmit)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = settings.EnableTransmit == enableTransmit
                ? settings
                : new AcousticSettingsRaw(
                        settings.SystemType,
                        settings.FrameRate,
                        settings.SampleCount,
                        settings.SampleStartDelay,
                        settings.SamplePeriod,
                        settings.PulseWidth,
                        settings.PingMode,
                        enableTransmit: enableTransmit,
                        settings.Frequency,
                        settings.Enable150Volts,
                        settings.ReceiverGain,
                        settings.FocusDistance,
                        settings.AntiAliasing,
                        settings.InterpacketDelay,
                        settings.Salinity);

            return result;
        }

        public static AcousticSettingsRaw UpdateAntiAliasing(
            AcousticSettingsRaw settings,
            FineDuration antiAliasing)

        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = settings.AntiAliasing == antiAliasing
            ? settings
            : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    antiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            return result;
        }

        public static AcousticSettingsRaw WithFocusDistance(
            this AcousticSettingsRaw settings,
            Distance newFocusDistance)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result =
                new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    newFocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity)
                .ApplyAllConstraints();

            return result;
        }

        public static AcousticSettingsRaw WithTransmitEnable(
            this AcousticSettingsRaw settings,
            bool enable)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = enable == settings.EnableTransmit
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    enable,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            return result;
        }

        public static AcousticSettingsRaw With150VoltsEnable(
            this AcousticSettingsRaw settings,
            bool enable)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = enable == settings.Enable150Volts
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    settings.SampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    enable,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            return result;
        }

        public static AcousticSettingsRaw WithSampleCount(
            this AcousticSettingsRaw settings,
            int sampleCount)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var result = sampleCount == settings.SampleCount
                ? settings
                : new AcousticSettingsRaw(
                    settings.SystemType,
                    settings.FrameRate,
                    sampleCount,
                    settings.SampleStartDelay,
                    settings.SamplePeriod,
                    settings.PulseWidth,
                    settings.PingMode,
                    settings.EnableTransmit,
                    settings.Frequency,
                    settings.Enable150Volts,
                    settings.ReceiverGain,
                    settings.FocusDistance,
                    settings.AntiAliasing,
                    settings.InterpacketDelay,
                    settings.Salinity);

            return result;
        }
    }
}
