using System;
using System.Globalization;
using System.Linq;

namespace UnnamedTestProgram
{
    using FieldName = String;
    delegate string SerializeField(string fieldName, in RawSettings settings);
    delegate void OptionDeserializeField(string value, ref RawSettings settings);

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
                var @this = this; // Can't use 'this' inside the lambda expression
                return fieldOps
                        .Select(ops => ops.SerializeField(ops.Name, @this))
                        .ToArray();
            }
            finally
            {
                CultureInfo.CurrentCulture = ambientCulture;
            }
        }

        public static RawSettings Deserialize(string s)
        {
            var ambientCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                var splits = s.Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length != fieldOps.Length)
                {
                    throw new ArgumentException("Wrong number of settings provided");
                }

                var settings = new RawSettings();

                // fieldOps is order-dependent here.
                foreach (var (ops, value) in fieldOps.Zip(splits))
                {
                    ops.OptionDeserializeField(value, ref settings);
                }

                return settings;
            }
            finally
            {
                CultureInfo.CurrentCulture = ambientCulture;
            }
        }

        private struct SerdesFieldOps
        {
            public FieldName Name;
            public SerializeField SerializeField;
            public OptionDeserializeField OptionDeserializeField;
        }

        private static readonly SerdesFieldOps[] fieldOps = new[]
        {
            new SerdesFieldOps
            {
                Name = "frame_rate",
                SerializeField = (string name, in RawSettings settings) => $"{name} {settings.FrameRate}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.FrameRate = float.Parse(value)
            },
            new SerdesFieldOps
            {
                Name = "samples_per_beam",
                SerializeField = (string name, in RawSettings settings) => $"{name} {settings.SamplesPerBeam}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.SamplesPerBeam = uint.Parse(value)
            },
            new SerdesFieldOps
            {
                Name = "sample_start_delay",
                SerializeField = (string name, in RawSettings settings) => $"{name} {settings.SampleStartDelay}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.SampleStartDelay = uint.Parse(value)
            },
            new SerdesFieldOps
            {
                Name = "cycle_period",
                SerializeField = (string name, in RawSettings settings) => $"{name} {settings.CyclePeriod}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.CyclePeriod = uint.Parse(value)
            },
            new SerdesFieldOps
            {
                Name = "sample_period",
                SerializeField = (string name, in RawSettings settings) => $"{name} {settings.SamplePeriod}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.SamplePeriod = uint.Parse(value)
            },
            new SerdesFieldOps
            {
                Name = "pulse_width",
                SerializeField = (string name, in RawSettings settings) => $"{name} {settings.PulseWidth}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.PulseWidth = uint.Parse(value)
            },
            new SerdesFieldOps
            {
                Name = "ping_mode",
                SerializeField = (string name, in RawSettings settings) => $"{name} {settings.PingMode}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.PingMode = uint.Parse(value)
            },
            new SerdesFieldOps
            {
                Name = "enable_transmit",
                SerializeField = (string name, in RawSettings settings) => $"{name} {Serialize(settings.EnableTransmit)}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.EnableTransmit = DeserializeBool(value)
            },
            new SerdesFieldOps
            {
                Name = "frequency",
                SerializeField = (string name, in RawSettings settings) => $"{name} {Serialize(settings.Frequency)}",
                OptionDeserializeField = DeserializeFrequency
            },
            new SerdesFieldOps
            {
                Name = "enable_150V",
                SerializeField = (string name, in RawSettings settings) => $"{name} {Serialize(settings.Enable150Volts)}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.Enable150Volts = DeserializeBool(value)
            },
            new SerdesFieldOps
            {
                Name = "receiver_gain",
                SerializeField = (string name, in RawSettings settings) => $"{name} {settings.ReceiverGain}",
                OptionDeserializeField =
                    (string value, ref RawSettings settings) => settings.ReceiverGain = float.Parse(value)
            },
        };

        private static string Serialize(RawSettingsFrequency frequency) =>
            frequency == RawSettingsFrequency.High ? "high" : "low";

        private static string Serialize(bool b) => b ? "true" : "false"; // lowercase

        private static void DeserializeFrequency(string value, ref RawSettings settings)
        {
            settings.Frequency = value.ToLower() switch
            {
                "high" => RawSettingsFrequency.High,
                "1" => RawSettingsFrequency.High,

                "low" => RawSettingsFrequency.Low,
                "0" => RawSettingsFrequency.Low,

                _ => throw new ArgumentOutOfRangeException($"Unrecognized frequency value [{value}]")
            };
        }

        private static bool DeserializeBool(string s) =>
            s switch
            {
                "1" => true,
                "0" => false,
                _ => bool.Parse(s)
            };
    }
}
