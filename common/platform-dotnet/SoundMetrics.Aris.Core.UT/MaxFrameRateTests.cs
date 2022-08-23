using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.Raw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public class MaxFrameRateTests
    {
        private struct Expecteds
        {
            public FineDuration CyclePeriod;
            public FineDuration MinimumFramePeriod;
            public Rate MaximuimFrameRate;

            public override string ToString() =>
                $"CyclePeriod=[{CyclePeriod}]; MinimumFramePeriod=[{MinimumFramePeriod}]";
        }

        private struct ExpectedIntermediates
        {
            public FineDuration MCP;
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
                Console.WriteLine($"Input: [{testCase}]");

                var settings = CreateTestSettings(testCase);
                var expectedIntermediates = testCase.ExpectedIntermediates;
                var expected = testCase.Expecteds;
                var expectedMaximumFrameRate = expected.MaximuimFrameRate;

                var maximumFrameRate =
                    MaxFrameRate.CalculateMaximumFrameRateWithIntermediates(
                        settings,
                        out var calculatedCyclePeriod,
                        out var intermediateResults);

                Console.WriteLine($"Calculated Cycle Period: [{calculatedCyclePeriod}]");
                Console.WriteLine($"Output intermediates: [{intermediateResults}]");
                Console.WriteLine($"Expected max frame rate: [{expectedMaximumFrameRate}]");
                Console.WriteLine($"Actual max frame rate: [{maximumFrameRate}]");

                Assert.AreEqual(
                    expectedIntermediates.MCP,
                    intermediateResults.MCP,
                    $"{nameof(intermediateResults.MCP)} {testCase.Description}");
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
                var variance = (expectedMaximumFrameRate - maximumFrameRate).Abs();

                Assert.IsTrue(variance <= tolerance,
                    $"Max frame rate Variance [{variance}] exceeds tolerance [{tolerance}];\n"
                    + $"toleranceRatio=[{toleranceRatio}];\n"
                    + $"expected=[{expectedMaximumFrameRate}]; actual=[{maximumFrameRate}]\n"
                    + $"{testCase.Description}");
            }
        }

        private const string TestCases =
            // System Type, SSD (input), SP (input), SPB (input), MCP (calc), AA (input), CP (output), PPF (input), ID (input), IDA (calc), MFP (µs), MFR (fps), MFPA (µs), MFRA (fps)

            // test inputs 5/31/2022, after implementing new max frame rate changes
            @"
                ARIS 3000, 1000, 5, 1500, 8920, 3000, 11920, 8, 10, 3689, 99049, 10.096061, 101189, 9.882497
                ARIS 1800, 1000, 4, 2000, 9420, 1000, 10420, 6, 0, 0, 62520, 15.994882, 67041, 14.916245
                ARIS 1200, 6000, 4, 4000, 22420, 6000, 28420, 3, 20, 5075, 90335, 11.069882, 92353, 10.828019

                ARIS 3000, 1000, 8, 2000, 17420, 0, 17420, 4, 0, 0, 69680, 14.351320, 71770, 13.933398
                ARIS 1800, 1000, 8, 3000, 25420, 0, 25420, 3, 0, 0, 76260, 13.113034, 78547, 12.731231
            ";

        private static class TestCaseParser
        {

            private const int SystemTypeIdx = 0;
            private const int SampleStartDelayIdx = 1;
            private const int SamplePeriodIdx = 2;
            private const int SampleCountIdx = 3;
            private const int MCPIdx = 4;
            private const int AntialiasingIdx = 5;
            private const int CyclePeriodIdx = 6;
            private const int PPFIdx = 7;
            private const int InterpacketDelayIdx = 8;
            private const int IDAIdx = 9;
            private const int MFPIdx = 10;
            private const int MFRAIdx = 13;

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
                    int linesReturned = 0;
                    var reader = new StringReader(input);

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string trimmedLine;
                        if (!string.IsNullOrWhiteSpace(trimmedLine = line.Trim()))
                        {
                            ++linesReturned;
                            yield return trimmedLine;
                        }
                    }

                    Assert.AreNotEqual(0, linesReturned);
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
                            MaximuimFrameRate = (Rate)double.Parse(fields[MFRAIdx]),
                        },

                        ExpectedIntermediates = new ExpectedIntermediates
                        {
                            MCP = (FineDuration)int.Parse(fields[MCPIdx]),
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
