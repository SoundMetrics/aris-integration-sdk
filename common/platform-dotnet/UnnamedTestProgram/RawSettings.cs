using System.Globalization;

namespace UnnamedTestProgram
{
    public enum RawSettingsFrequency
    {
        Low = 0,
        High = 1,
    }

    public struct RawSettings
    {
        public float FrameRate;
        public uint SamplesPerBeam;
        public uint SampleStartDelay;
        public uint CyclePeriod;
        public uint SamplePeriod;
        public uint PulseWidth;
        public uint PingMode;
        public bool EnableTransmit;
        public RawSettingsFrequency Frequency;
        public bool Enable150Volts;
        public float ReceiverGain;

        public string[] Serialize()
        {
            var ambientCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                return new[]
                {
                    $"frame_rate {FrameRate}",
                    $"samples_per_beam {SamplesPerBeam}",
                    $"sample_start_delay {SampleStartDelay}",
                    $"cycle_period {CyclePeriod}",
                    $"sample_period {SamplePeriod}",
                    $"pulse_width {PulseWidth}",
                    $"ping_mode {PingMode}",
                    $"enable_transmit {EnableTransmit}",
                    $"frequency {Frequency}",
                    $"enable_150V {Enable150Volts}",
                    $"receiver_gain {ReceiverGain}",
                };
            }
            finally
            {
                CultureInfo.CurrentCulture = ambientCulture;
            }
        }
    }
}
