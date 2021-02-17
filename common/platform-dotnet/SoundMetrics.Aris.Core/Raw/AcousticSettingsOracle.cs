﻿// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Runtime.CompilerServices;

namespace SoundMetrics.Aris.Core.Raw
{
    using static AcousticSettingsConstraints;

    public static class AcousticSettingsOracle
    {
        /// <summary>
        /// Initializes a new instance of acoustic settings.
        /// </summary>
        public static AcousticSettingsRaw Initialize(
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
            return allowed;
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

        public static AcousticSettingsRaw SetFrameRate(
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

            return UpdateFrameRate(settings, allowedFrameRate);
        }

        public static AcousticSettingsRaw SetMaxFrameRate(
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

        public static AcousticSettingsRaw SetFocusPosition(
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
            return newSettings;
        }

        public static AcousticSettingsRaw SetAutomatedFocus(this AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            return SetFocusPosition(settings, settings.WindowMidPoint);
        }

        public static AcousticSettingsRaw SetFrequency(
            this AcousticSettingsRaw settings,
            Frequency frequency)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            return new AcousticSettingsRaw(
                settings.SystemType,
                settings.FrameRate,
                settings.SampleCount,
                settings.SampleStartDelay,
                settings.CyclePeriod,
                settings.SamplePeriod,
                settings.PulseWidth,
                settings.PingMode,
                settings.EnableTransmit,
                frequency,
                settings.Enable150Volts,
                settings.ReceiverGain,
                settings.FocusPosition,
                settings.AntiAliasing,
                settings.InterpacketDelay,
                settings.SonarEnvironment);
        }

        private static AcousticSettingsRaw SetAutomatedFrequency(this AcousticSettingsRaw settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            var sysCfg = SystemConfiguration.GetConfiguration(settings.SystemType);
            var isLongerRange = settings.WindowEnd > sysCfg.FrequencyCrossover;
            var frequency = isLongerRange ? Frequency.High : Frequency.Low;
            return settings.SetFrequency(frequency);
        }

        public static AcousticSettingsRaw SetInterpacketDelay(
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
                useMaxFrameRate ? newSettings.SetMaxFrameRate() : newSettings;
            return result;
        }

        public static AcousticSettingsRaw SetReceiverGain(
            this AcousticSettingsRaw settings,
            float gain)
        {
            throw new NotImplementedException();
        }

        //public static AcousticSettingsRaw SetTransmitEnable(
        //    this AcousticSettingsRaw settings,
        //    bool enable)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRaw Set150VoltsEnable(
        //    this AcousticSettingsRaw settings,
        //    bool enable)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRaw SetSampleCount(
        //    this AcousticSettingsRaw settings,
        //    int sampleCount)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRaw SetFrequency(
        //    this AcousticSettingsRaw settings,
        //    Frequency frequency)
        //    => throw new NotImplementedException();

        //public static AcousticSettingsRaw SetPulseWidth(
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
