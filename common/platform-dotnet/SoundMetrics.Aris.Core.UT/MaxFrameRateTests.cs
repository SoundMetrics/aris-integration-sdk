using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.Raw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoundMetrics.Aris.Core.UT
{
    using static System.Math;

    [TestClass]
    public class MaxFrameRateTests
    {
        private struct Expecteds
        {
            public FineDuration CyclePeriod;
            public FineDuration MinimumFramePeriod;

            public override string ToString() =>
                $"CyclePeriod=[{CyclePeriod}]; MinimumFramePeriod=[{MinimumFramePeriod}]";
        }

        private struct ExpectedIntermediates
        {
            public FineDuration MCP;
            public FineDuration CPA;
            public int PPF;

            public override string ToString() => $"MCP=[{MCP}]; PPF=[{PPF}]";
        }

        private struct TestCase
        {
            public string Description;

            public SystemType SystemType;
            public PingMode PingMode;
            public int SampleCount;
            public FineDuration SampleStartDelay;
            public FineDuration SamplePeriod;
            public FineDuration Antialiasing;
            public InterpacketDelaySettings InterpacketDelay;

            public Expecteds Expecteds;
            public ExpectedIntermediates ExpectedIntermediates;

            public override string ToString() =>
                $"SystemType={SystemType}; "
                + $"PingMode={PingMode}; "
                + $"SampleCount={SampleCount}; "
                + $"SampleStartDelay={SampleStartDelay}; "
                + $"SamplePeriod={SamplePeriod}; "
                + $"Antialiasing={Antialiasing}; "
                + $"InterpacketDelay={InterpacketDelay}; "
                + $"Expecteds={Expecteds}; "
                + $"ExpectedIntermediates={ExpectedIntermediates}; ";
        }

        private static AcousticSettingsRaw CreateTestSettings(
            in TestCase testCase)
        {
            return new AcousticSettingsRaw(
                testCase.SystemType,
                frameRate: (Rate)1,
                sampleCount: testCase.SampleCount,
                sampleStartDelay: testCase.SampleStartDelay,
                samplePeriod: testCase.SamplePeriod,
                pulseWidth: (FineDuration)13,
                testCase.PingMode,
                enableTransmit: true,
                Frequency.High,
                enable150Volts: true,
                receiverGain: 12,
                focusDistance: (Distance)((2 + 7.6) / 2),
                antiAliasing: testCase.Antialiasing,
                testCase.InterpacketDelay,
                Salinity.Fresh);
        }

        [TestMethod]
        public void RunMultipleCases()
        {
            foreach (var testCase in TestCaseParser.ParseTestCases(TestCases))
            {
                var settings = CreateTestSettings(testCase);
                var expectedIntermediates = testCase.ExpectedIntermediates;
                var expected = testCase.Expecteds;
                var expectedMaximumFrameRate =
                    (1 / expected.MinimumFramePeriod)
                        .Hz
                        .ConstrainTo(SystemConfigurationRaw.MaxFrameRateRange);

                var maximumFrameRate =
                    MaxFrameRate.DetermineMaximumFrameRateWithIntermediates(
                        settings,
                        out var calculatedCyclePeriod,
                        out var intermediateResults);

                Assert.AreEqual(
                    expectedIntermediates.MCP,
                    intermediateResults.MCP,
                    $"{nameof(intermediateResults.MCP)} {testCase.Description}");
                Assert.AreEqual(
                    expectedIntermediates.CPA,
                    intermediateResults.CPA1,
                    $"{nameof(intermediateResults.CPA1)} {testCase.Description}");
                Assert.AreEqual(
                    expectedIntermediates.PPF,
                    intermediateResults.PPF,
                    $"{nameof(intermediateResults.PPF)} {testCase.Description}");

                Assert.AreEqual(
                    expected.CyclePeriod.RoundToMicroseconds(),
                    calculatedCyclePeriod.RoundToMicroseconds(),
                    testCase.Description);

                var toleranceRatio = 0.01;

                var tolerance = expectedMaximumFrameRate * toleranceRatio;
                var variance = Abs(expectedMaximumFrameRate - maximumFrameRate.Hz);

                Assert.IsTrue(variance <= tolerance,
                    $"Variance [{variance}] exceeds tolerance [{tolerance}];\n"
                    + $"toleranceRatio=[{toleranceRatio}];\n"
                    + $"expected=[{expectedMaximumFrameRate}]; actual=[{maximumFrameRate}]\n"
                    + $"{testCase.Description}");
            }
        }

        private const string TestCases =
            // System Type, SSD (input), SP (input), SPB (input), MCP (calc), AA (input), CPA (calc), CP (output), PPF (input), ID (input), IDA (calc), MFP (µs), MFR (fps)
            @"
                ARIS 3000, 2626, 8, 1250, 13046, 0, 391, 13437, 8, 0, 0, 107496, 9.30
                ARIS 1800, 2626, 8, 1250, 13046, 0, 391, 13437, 6, 0, 0, 80622, 12.40
                ARIS 1200, 2626, 8, 1250, 13046, 0, 260, 13306, 3, 0, 0, 39918, 25.05

                ARIS 3000, 2626, 8, 1250, 13046, 0, 391, 13437, 4, 0, 0, 53748, 18.61
                ARIS 1800, 2626, 8, 1250, 13046, 0, 391, 13437, 3, 0, 0, 40311, 24.81

                ARIS 3000, 2626, 8, 1250, 13046, 2345, 2736, 15782, 8, 0, 0, 126256, 7.92
                ARIS 1800, 2626, 8, 1250, 13046, 2345, 2736, 15782, 6, 0, 0, 94692, 10.56
                ARIS 1200, 2626, 8, 1250, 13046, 2345, 2605, 15651, 3, 0, 0, 46953, 21.30

                ARIS 3000, 2626, 8, 1250, 13046, 2345, 2736, 15782, 4, 0, 0, 63128, 15.84
                ARIS 1800, 2626, 8, 1250, 13046, 2345, 2736, 15782, 3, 0, 0, 47346, 21.12
            ";

        private static class TestCaseParser
        {

            private const int SystemTypeIdx = 0;
            private const int SampleStartDelayIdx = 1;
            private const int SamplePeriodIdx = 2;
            private const int SampleCountIdx = 3;
            private const int MCPIdx = 4;
            private const int AntialiasingIdx = 5;
            private const int CPAIdx = 6;
            private const int CyclePeriodIdx = 7;
            private const int PPFIdx = 8;
            private const int InterpacketDelayIdx = 9;
            private const int IDAIdx = 10;
            private const int MFPIdx = 11;
            // private const int MFRIdx = 12; min(15, 1/MFP)

            private static readonly Dictionary<string, SystemType> systemTypeLookup =
                new Dictionary<string, SystemType>
                {
                    { "ARIS 3000", SystemType.Aris3000 },
                    { "ARIS 1800", SystemType.Aris1800 },
                    { "ARIS 1200", SystemType.Aris1200 },
                };

            public static IEnumerable<TestCase> ParseTestCases(string input)
            {
                foreach (var line in GetNonEmptyLines())
                {
                    yield return ParseTestCase(line);
                }

                IEnumerable<string> GetNonEmptyLines()
                {
                    var reader = new StringReader(input);

                    string line;
                    while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
                    {
                        string trimmedLine;
                        if (!string.IsNullOrWhiteSpace(trimmedLine = line.Trim()))
                        {
                            yield return trimmedLine;
                        }
                    }
                }

                static TestCase ParseTestCase(string line)
                {
                    var fields = line.Split(",").Select(CleanField).ToArray();

                    var systemType = systemTypeLookup[fields[SystemTypeIdx]];

                    return new TestCase
                    {
                        Description = $"Test case input=[{line}]",
                        SystemType = systemType,
                        PingMode = PingModeFromPPF(systemType, int.Parse(fields[PPFIdx])),
                        SampleCount = int.Parse(fields[SampleCountIdx]),
                        SampleStartDelay = (FineDuration)int.Parse(fields[SampleStartDelayIdx]),
                        SamplePeriod = (FineDuration)int.Parse(fields[SamplePeriodIdx]),
                        Antialiasing = (FineDuration)int.Parse(fields[AntialiasingIdx]),
                        InterpacketDelay = ParseInterpacketDelay(fields[InterpacketDelayIdx]),

                        Expecteds = new Expecteds
                        {
                            CyclePeriod = (FineDuration)int.Parse(fields[CyclePeriodIdx]),
                            MinimumFramePeriod = (FineDuration)int.Parse(fields[MFPIdx]),
                        },

                        ExpectedIntermediates = new ExpectedIntermediates
                        {
                            MCP = (FineDuration)int.Parse(fields[MCPIdx]),
                            CPA = (FineDuration)int.Parse(fields[CPAIdx]),
                            PPF = int.Parse(fields[PPFIdx]),
                        },
                    };
                }

                static string CleanField(string field) => field.Trim();

                static PingMode PingModeFromPPF(SystemType systemType, int ppf)
                {
                    foreach (var pingMode in systemType.GetConfiguration().AvailablePingModes)
                    {
                        if (pingMode.PingsPerFrame == ppf)
                        {
                            return pingMode;
                        }
                    }

                    throw new ArgumentOutOfRangeException(
                        $"[{ppf}] is not a valid ping count for [{systemType}]");
                }

                static InterpacketDelaySettings ParseInterpacketDelay(string field)
                {
                    var value = int.Parse(field);
                    return value == 0
                        ? InterpacketDelaySettings.Off
                        : new InterpacketDelaySettings { Delay = (FineDuration)value, Enable = true };
                }
            }
        }
    }
}
