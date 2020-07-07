using SoundMetrics.Aris.Data;
using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Device
{
    public sealed class InvalidSonarConfig : Exception
    {
        public InvalidSonarConfig(string message) : base(message) { }
    }

    internal static class SonarConfig
    {
        // Min/max values per ARIS Engineering Test Command List

        internal static class Values
        {
            public static readonly ValueRange<int> SampleCountRange = new ValueRange<int>(200, 4000);
            public static readonly ValueRange<int> FocusPositionRange = new ValueRange<int>(0, 1000);
            public static readonly ValueRange<int> ReceiverGainRange = new ValueRange<int>(0, 24);
            public static readonly ValueRange<double> FrameRateRange = new ValueRange<double>(1.0, 15.0);

            public static readonly ValueRange<int> SampleStartDelayRange = new ValueRange<int>(930, 60000);
            public static readonly ValueRange<int> CyclePeriodRange = new ValueRange<int>(1802, 150000);
            public static readonly ValueRange<int> SamplePeriodRange = new ValueRange<int>(4, 100);
            public static readonly ValueRange<int> PulseWidthRange = new ValueRange<int>(4, 80);

            public static readonly ValueRange<double> WindowStartRange = new ValueRange<double>(0.7, 40.0);
            public static readonly ValueRange<double> WindowEndRange = new ValueRange<double>(1.3, 100.0);

            public static readonly int CyclePeriodMargin = 360;
            public static readonly int MinAntialiasing = 0;
        }

        public struct UsableImagingRange
        {
            public Dictionary<Frequency, ValueRange<double>> UsableRange;
            public double LowFrequencyCrossover;
        }

        public struct OperationalRanges
        {
            public SystemType SystemType;
            public ValueRange<int> PulseWidthRange;
            public ValueRange<int> SampleStartDelayRange;
            public ValueRange<int> SamplePeriodRange;
            public ValueRange<int> CyclePeriodRange;
            public ValueRange<double> WindowStartRange;
            public ValueRange<double> WindowEndRange;
            public UsableImagingRange UsableImagingRange;

            public double MaxRange => WindowEndRange.Max;
        }

        private static Dictionary<SystemType, OperationalRanges> SystemTypeRangeMap =
            new Dictionary<SystemType, OperationalRanges>()
            {
                {
                    SystemType.Aris1800,
                    new OperationalRanges
                    {
                        PulseWidthRange =       Values.PulseWidthRange.Constrain(null,          40),
                        SampleStartDelayRange = Values.SampleStartDelayRange.Constrain(null,    36_000),
                        SamplePeriodRange =     Values.SamplePeriodRange.Constrain(null,        32),
                        CyclePeriodRange =      Values.CyclePeriodRange.Constrain(null,         80_000),
                        WindowStartRange =      Values.WindowStartRange.Constrain(null,         25.0),
                        WindowEndRange =        Values.WindowEndRange.Constrain(null,           50.0),

                        UsableImagingRange = new UsableImagingRange
                        {
                            UsableRange = new Dictionary<Frequency, ValueRange<double>>()
                            {
                                { Frequency.High, new ValueRange<double>(5.0, 20.0) },
                                { Frequency.Low,  new ValueRange<double>(20.0, 50.0) },
                            },
                            LowFrequencyCrossover = 15.0,
                        },
                    }
                },

                {
                    SystemType.Aris3000,
                    new OperationalRanges
                    {
                        PulseWidthRange =       Values.PulseWidthRange.Constrain(null,          24),
                        SampleStartDelayRange = Values.SampleStartDelayRange.Constrain(null,    18_000),
                        SamplePeriodRange =     Values.SamplePeriodRange.Constrain(null,        26),
                        CyclePeriodRange =      Values.CyclePeriodRange.Constrain(null,         40_000),
                        WindowStartRange =      Values.WindowStartRange.Constrain(null,         12.0),
                        WindowEndRange =        Values.WindowEndRange.Constrain(null,           20.0),

                        UsableImagingRange = new UsableImagingRange
                        {
                            UsableRange = new Dictionary<Frequency, ValueRange<double>>()
                            {
                                { Frequency.High, new ValueRange<double>(1.0, 8.0) },
                                { Frequency.Low,  new ValueRange<double>(8.0, 20.0) },
                            },
                            LowFrequencyCrossover = 5.0,
                        },
                    }
                },

                {
                    SystemType.Aris1200,
                    new OperationalRanges
                    {
                        PulseWidthRange =       Values.PulseWidthRange.Constrain(null,          80),
                        SampleStartDelayRange = Values.SampleStartDelayRange.Constrain(null,    60_000),
                        SamplePeriodRange =     Values.SamplePeriodRange.Constrain(null,        40),
                        CyclePeriodRange =      Values.CyclePeriodRange.Constrain(null,         150_000),
                        WindowStartRange =      Values.WindowStartRange.Constrain(null,         40.0),
                        WindowEndRange =        Values.WindowEndRange.Constrain(null,           100.0),

                        UsableImagingRange = new UsableImagingRange
                        {
                            UsableRange = new Dictionary<Frequency, ValueRange<double>>()
                            {
                                { Frequency.High, new ValueRange<double>(10.0, 30) },
                                { Frequency.Low,  new ValueRange<double>(30.0, 100.0) },
                            },
                            LowFrequencyCrossover = 25.0,
                        }
                    }
                },
            };

        public struct PingModeDefinition
        {
            public PingMode PingMode;
            public int ChannelCount;
            public int PingsPerFrame;
        }

        private static readonly PingModeDefinition[] PingModeDefinitions =
        {
            // Indexing from zero
            new PingModeDefinition { PingMode = PingMode.Invalid(0),  ChannelCount =   0,  PingsPerFrame = 0 },
            new PingModeDefinition { PingMode = PingMode.PingMode1,   ChannelCount =  48,  PingsPerFrame = 3 },
            new PingModeDefinition { PingMode = PingMode.Invalid(2),  ChannelCount =  48,  PingsPerFrame = 1 },
            new PingModeDefinition { PingMode = PingMode.PingMode3,   ChannelCount =  96,  PingsPerFrame = 6 },
            new PingModeDefinition { PingMode = PingMode.Invalid(4),  ChannelCount =  96,  PingsPerFrame = 2 },
            new PingModeDefinition { PingMode = PingMode.Invalid(5),  ChannelCount =  96,  PingsPerFrame = 1 },
            new PingModeDefinition { PingMode = PingMode.PingMode6,   ChannelCount =  64,  PingsPerFrame = 4 },
            new PingModeDefinition { PingMode = PingMode.Invalid(7),  ChannelCount =  64,  PingsPerFrame = 2 },
            new PingModeDefinition { PingMode = PingMode.Invalid(8),  ChannelCount =  64,  PingsPerFrame = 1 },
            new PingModeDefinition { PingMode = PingMode.PingMode9,   ChannelCount = 128,  PingsPerFrame = 8 },
            new PingModeDefinition { PingMode = PingMode.Invalid(10), ChannelCount = 128,  PingsPerFrame = 4 },
            new PingModeDefinition { PingMode = PingMode.Invalid(11), ChannelCount = 128,  PingsPerFrame = 2 },
            new PingModeDefinition { PingMode = PingMode.Invalid(12), ChannelCount = 128,  PingsPerFrame = 1 },
        };

        struct SystemTypePingModeInfo
        {
            public SystemType SystemType;
            public PingMode DefaultPingMode;
            public PingMode[] ValidPingModes;
        };

        private static SystemTypePingModeInfo[] SystemTypePingModeInfos =
        {
            // Indexed by SystemType
            new SystemTypePingModeInfo
            {
                SystemType = SystemType.Aris1800,
                DefaultPingMode = PingMode.PingMode3,
                ValidPingModes = new [] { PingMode.PingMode1, PingMode.PingMode3 },
            },
            new SystemTypePingModeInfo
            {
                SystemType = SystemType.Aris3000,
                DefaultPingMode = PingMode.PingMode9,
                ValidPingModes = new [] { PingMode.PingMode6, PingMode.PingMode9 },
            },
            new SystemTypePingModeInfo
            {
                SystemType = SystemType.Aris1200,
                DefaultPingMode = PingMode.PingMode1,
                ValidPingModes = new [] { PingMode.PingMode1 },
            },
        };

        public static PingModeDefinition GetPingModeDefinition(PingMode pingMode)
        {
            pingMode.AssertValid();
            var integralPingMode = pingMode.IntegralValue;

            if (integralPingMode < 0 || PingModeDefinitions.Length <= integralPingMode)
            {
                throw new ArgumentOutOfRangeException(nameof(integralPingMode));
            }

            return PingModeDefinitions[integralPingMode];
        }

        public static PingMode GetDefaultPingModeForSystemType(SystemType systemType)
        {
            return SystemTypePingModeInfos[(int)systemType].DefaultPingMode;
        }
    }
}
