using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.Raw;

namespace SoundMetrics.Aris.Core.UT
{
    using static System.Math;

    [TestClass]
    public class MaxFrameRateTests
    {
        private static AcousticSettingsRaw AcousticSettings =
            new AcousticSettingsRaw(
            SystemType.Aris3000,
            frameRate: (Rate)9.3,
            sampleCount: 1250,
            sampleStartDelay: (FineDuration)2626,
            samplePeriod: (FineDuration)8,
            pulseWidth: (FineDuration)13,
            PingMode.PingMode9,
            enableTransmit: true,
            Frequency.High,
            enable150Volts: true,
            receiverGain: 12,
            focusDistance: (Distance)((2+7.6)/2),
            antiAliasing: (FineDuration)0,
            InterpacketDelaySettings.Off,
            Salinity.Fresh // Not supplied in issue
            );

        private struct Expecteds
        {
            public FineDuration CyclePeriod;
            public FineDuration MinimumFramePeriod;
        }

        private struct ExpectedIntermediates
        {
            public FineDuration MCP;
            public FineDuration CPA;
            public int PPF;
        }

        private struct TestCase
        {
            public string Description;

            public SystemType SystemType;
            public PingMode PingMode;
            public FineDuration Antialiasing;

            public Expecteds Expecteds;
            public ExpectedIntermediates ExpectedIntermediates;
        }

        private static readonly TestCase[] testCases = new []
        {
            // ARIS 3000

            new TestCase
            {
                Description = "ARIS 3000 8 ping cycles, no AA",
                SystemType = SystemType.Aris3000,
                PingMode = PingMode.PingMode9,
                Antialiasing = (FineDuration)0,
                ExpectedIntermediates =
                    new ExpectedIntermediates
                        { MCP = (FineDuration)13046, CPA = (FineDuration)391,
                          PPF = 8 },
                Expecteds =
                    new Expecteds
                    { CyclePeriod = (FineDuration)13437,
                      MinimumFramePeriod = (FineDuration)107496 },
            },
            new TestCase
            {
                Description = "ARIS 3000 4 ping cycles, no AA",
                SystemType = SystemType.Aris3000,
                PingMode = PingMode.PingMode6,
                Antialiasing = (FineDuration)0,
                ExpectedIntermediates =
                    new ExpectedIntermediates
                        { MCP = (FineDuration)13046, CPA = (FineDuration)391,
                          PPF = 4},
                Expecteds =
                    new Expecteds
                    { CyclePeriod = (FineDuration)13437,
                      MinimumFramePeriod = (FineDuration)53748, },
            },

            new TestCase
            {
                Description = "ARIS 3000 8 ping cycles, w/AA",
                SystemType = SystemType.Aris3000,
                PingMode = PingMode.PingMode9,
                Antialiasing = (FineDuration)2345,
                ExpectedIntermediates =
                    new ExpectedIntermediates
                        { MCP = (FineDuration)13046, CPA = (FineDuration)2736,
                          PPF = 8 },
                Expecteds =
                    new Expecteds
                    { CyclePeriod = (FineDuration)15782,
                      MinimumFramePeriod = (FineDuration)126256 },
            },
            new TestCase
            {
                Description = "ARIS 3000 4 ping cycles, w/AA",
                SystemType = SystemType.Aris3000,
                PingMode = PingMode.PingMode6,
                Antialiasing = (FineDuration)2345,
                ExpectedIntermediates =
                    new ExpectedIntermediates
                        { MCP = (FineDuration)13046, CPA = (FineDuration)2736,
                          PPF = 4},
                Expecteds =
                    new Expecteds
                    { CyclePeriod = (FineDuration)15782,
                      MinimumFramePeriod = (FineDuration)63128, },
            },

        };

        private static AcousticSettingsRaw CreateTestSettings(
            in TestCase testCase)
        {
            return new AcousticSettingsRaw(
                testCase.SystemType,
                frameRate: (Rate)1,
                sampleCount: 1250,
                sampleStartDelay: (FineDuration)2626,
                samplePeriod: (FineDuration)8,
                pulseWidth: (FineDuration)13,
                testCase.PingMode,
                enableTransmit: true,
                Frequency.High,
                enable150Volts: true,
                receiverGain: 12,
                focusDistance: (Distance)((2 + 7.6) / 2),
                antiAliasing: testCase.Antialiasing,
                InterpacketDelaySettings.Off,
                Salinity.Fresh);
        }

        [TestMethod]
        public void RunMultipleCases()
        {
            foreach (var testCase in testCases)
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
    }
}
