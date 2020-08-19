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
                var frequency =
                    Frequency == RawSettingsFrequency.High
                        ? "high"
                        : "low";
                var enableTransmit = EnableTransmit ? "true" : "false";
                var enable150V = Enable150Volts ? "true" : "false";

                return new[]
                {
                    $"frame_rate {FrameRate}",
                    $"samples_per_beam {SamplesPerBeam}",
                    $"sample_start_delay {SampleStartDelay}",
                    $"cycle_period {CyclePeriod}",
                    $"sample_period {SamplePeriod}",
                    $"pulse_width {PulseWidth}",
                    $"ping_mode {PingMode}",
                    $"enable_transmit {enableTransmit}",
                    $"frequency {frequency}",
                    $"enable_150V {enable150V}",
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
