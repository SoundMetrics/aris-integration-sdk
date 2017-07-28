// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

using System.Collections.Generic;
using static SoundMetrics.Aris2.Protocols.ArisCommands;

namespace TestAris2Commands
{
    // These are the acoustic settings used in the tests herein.
    public static class TestAcousticSettings
    {
        public static AcousticSettingsWithCookie GetAcousticSettings(string systemType)
        {
            return _systemTypeMap[systemType];
        }

        static TestAcousticSettings()
        {
            _systemTypeMap = new Dictionary<string, AcousticSettingsWithCookie>
            {
                { "1800", new AcousticSettingsWithCookie {
                        Cookie = 0,
                        FrameRate = 15.0f,
                        PingMode = 3,
                        HighFrequency = true,
                        SampleCount = 1024,
                        SampleStartDelay = 2028,
                        CyclePeriod = 10500,
                        SamplePeriod = 8,
                        PulseWidth = 11,
                        EnableTransmit = true,
                        Enable150Volts = true,
                        ReceiverGain = 18,
                    }
                },
                { "3000", new AcousticSettingsWithCookie {
                        Cookie = 0,
                        FrameRate = 15.0f,
                        PingMode = 9,
                        HighFrequency = true,
                        SampleCount = 946,
                        SampleStartDelay = 2028,
                        CyclePeriod = 7118,
                        SamplePeriod = 5,
                        PulseWidth = 10,
                        EnableTransmit = true,
                        Enable150Volts = true,
                        ReceiverGain = 12,
                    }
                },
                { "1200", new AcousticSettingsWithCookie {
                        Cookie = 0,
                        FrameRate = 10.0f,
                        PingMode = 1,
                        HighFrequency = true,
                        SampleCount = 1082,
                        SampleStartDelay = 5408,
                        CyclePeriod = 32818,
                        SamplePeriod = 25,
                        PulseWidth = 24,
                        EnableTransmit = true,
                        Enable150Volts = true,
                        ReceiverGain = 20,
                    }
                },
            };
        }

        private static readonly Dictionary<string, AcousticSettingsWithCookie> _systemTypeMap;
    }
}
