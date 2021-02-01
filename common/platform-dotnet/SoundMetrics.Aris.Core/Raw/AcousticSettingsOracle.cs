// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core.Raw
{
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
            FocusPosition focusPosition,
            FineDuration antiAliasing,
            InterpacketDelaySettings interpacketDelay,
            EnvironmentalContext sonarEnvironment)
        {
            // This method is the only route to a new instance of AcousticSettingsRaw.
            // This promotes strong control over the values in AcousticSettingsRaw.

            var requested = new AcousticSettingsRaw(
                systemType,
                frameRate,
                samplesPerBeam,
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
            var allowed = ApplyConstraints(requested);

            return new AcousticSettingsRequest(requested, allowed);
        }

        private static AcousticSettingsRaw ApplyConstraints(AcousticSettingsRaw requested)
        {
            throw new NotImplementedException();
        }
    }
}
