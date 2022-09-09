using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.Raw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoundMetrics.Aris.Core
{
    using static SoundMetrics.Aris.Core.Helpers;
    using static Math;
    using TestCase = GuidedSettingsBaseTest_TestCase;

    public sealed class GuidedSettingsBaseTest_TestCase
    {
        // System configuration

        public SystemType SystemType;
        public PingMode PingMode;

        // User input

        public WindowBounds WindowBounds;

        // Environmental

        public Salinity Salinity;
        public ObservedConditions ObservedConditions;

        // Expected Outputs

        public Frequency Frequency;
        public FineDuration PulseWidth;
        public Velocity SoundSpeed; // just for check against documented figures
        public FineDuration SampleStartDelay;
        public FineDuration SamplePeriod;
        public int SampleCount;
        public FineDuration CyclePeriod;
        public Rate MaximumFrameRate;
    }

    // Deferred [TestClass]
    public sealed class GuidedSettingsBaseTest
    {
        [TestMethod]
        public void AllCases()
        {
            var testCases =
                LoadInputLinesFromFile()
                    .Select(ConvertToTestCase)
                    .ToArray();

            int idxTestCase = 0;
            foreach (var testCase in testCases)
            {
                Console.WriteLine($">>> Starting test case [idxTestCase={idxTestCase}]");
                RunTestCase(idxTestCase, testCase);

                ++idxTestCase;
            }

            static void RunTestCase(int idxTestCase, TestCase tc)
            {
                var startSettings =
                    tc.SystemType
                        .GetConfiguration()
                        .GetDefaultSettings(tc.ObservedConditions, tc.Salinity)
                        .WithPingMode(tc.PingMode);

                Assert.AreEqual(startSettings.SystemType, tc.SystemType);
                Assert.AreEqual(startSettings.PingMode, tc.PingMode);
                Assert.AreEqual(startSettings.Salinity, tc.Salinity);

                Console.WriteLine($"WindowBounds=[{tc.WindowBounds}]");
                Console.WriteLine($"SystemType=[{tc.SystemType}]; Salinity=[{tc.Salinity}]; conditions=[{tc.ObservedConditions}]");

                var result =
                    startSettings
                        .GetSettingsForSpecificRange(
                            GuidedSettingsMode.GuidedSampleCount,
                            tc.ObservedConditions,
                            tc.WindowBounds,
                            useMaxFrameRate: true,
                            useAutoFrequency: true);

                // verify test case doc's assumption
                AssertWithinRatio(
                    (double)tc.SoundSpeed,
                    (double)tc.ObservedConditions.SpeedOfSound(tc.Salinity),
                    1 / 1000.0,
                    $"idxTestCase=[{idxTestCase}]; property=[{nameof(tc.SoundSpeed)}]");

                // Result window
                var resultBounds = result.WindowBounds(tc.ObservedConditions);
                Console.WriteLine($"--- resultBounds=[{resultBounds}]");

                // Continue

                Assert.AreEqual(tc.Frequency, result.Frequency, nameof(tc.Frequency));

                AssertWithinRatio(
                    tc.PulseWidth.TotalMicroseconds,
                    result.PulseWidth.TotalMicroseconds,
                    1 / 1000.0,
                    $"idxTestCase=[{idxTestCase}]; property=[{nameof(tc.PulseWidth)}]");

                AssertWithinRatio(
                    tc.SampleStartDelay.TotalMicroseconds,
                    result.SampleStartDelay.TotalMicroseconds,
                    1 / 1000.0,
                    $"idxTestCase=[{idxTestCase}]; property=[{nameof(tc.SampleStartDelay)}]");

                Assert.AreEqual(tc.SamplePeriod, result.SamplePeriod);

                AssertWithinRatio(
                    tc.SampleCount,
                    result.SampleCount,
                    1 / 1000.0,
                    $"idxTestCase=[{idxTestCase}]; property=[{nameof(tc.SampleCount)}]");

                AssertWithinRatio(
                    tc.CyclePeriod.TotalMicroseconds,
                    result.CyclePeriod.TotalMicroseconds,
                    1 / 1000.0,
                    $"idxTestCase=[{idxTestCase}]; property=[{nameof(tc.CyclePeriod)}]");

                // Maximum frame rate
                Assert.AreEqual((FineDuration)0, result.AntiAliasing);
                Assert.AreEqual(default(InterpacketDelaySettings), result.InterpacketDelay);
                {
                    var maxRate =
                        MaxFrameRate.CalculateMaximumFrameRateWithIntermediates(
                            startSettings, out var cyclePeriod, out var _);
                    AssertWithinRatio(
                        tc.CyclePeriod.TotalMicroseconds,
                        result.CyclePeriod.TotalMicroseconds,
                        1 / 1000.0,
                        $"idxTestCase=[{idxTestCase}]; property=[{nameof(tc.CyclePeriod)}]");
                }

                AssertWithinRatio(
                    tc.MaximumFrameRate.Hz,
                    result.MaximumFrameRate.Hz,
                    1 / 1000.0,
                    $"idxTestCase=[{idxTestCase}]; property=[{nameof(tc.MaximumFrameRate)}]");
            }

            static void AssertWithinRatio(
                double expected,
                double actual,
                double ratio,
                string message)
            {
                var diff = Abs(expected - actual);
                var allowedDelta = Abs(expected * ratio);

                Assert.AreEqual(expected, actual, allowedDelta, message);
            }
        }

        private const string InputFileName =
            "Guided Settings Test Cases--Fresh Water.txt";

        private static TestCase ConvertToTestCase(string inputLine)
        {
            var splits = inputLine.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            var systemTypeLookup =
                    MakeReadOnlyDictionary(
                        ("ARIS 3000", SystemType.Aris3000),
                        ("ARIS 1800", SystemType.Aris1800),
                        ("ARIS 1200", SystemType.Aris1200));
            var frequencyLookup =
                MakeReadOnlyDictionary(("LF", Frequency.Low), ("HF", Frequency.High));

            var testCase = new TestCase
            {
                SystemType = systemTypeLookup[splits[0]],
                PingMode = PingMode.GetFrom(ParseInt(1)),
                WindowBounds = new WindowBounds(double.Parse(splits[4]), double.Parse(splits[5])),
                Salinity = (Salinity)ParseInt(6),
                ObservedConditions = new ObservedConditions(
                    (Temperature)double.Parse(splits[7]),
                    Distance.Zero), // document's assumption for calculating sspd
                Frequency = frequencyLookup[splits[9]],
                PulseWidth = (FineDuration)ParseInt(12),
                SoundSpeed = (Velocity)double.Parse(splits[13]),
                SampleStartDelay = (FineDuration)ParseInt(14),
                SamplePeriod = (FineDuration)ParseInt(17),
                SampleCount = ParseInt(18),
                CyclePeriod = (FineDuration)ParseInt(19),
                MaximumFrameRate = (Rate)double.Parse(splits[21]),
            };

            return testCase;

            // For ease of debugging test development
            int ParseInt(int splitIndex)
            {
                var value = splits[splitIndex];
                try
                {
                    return int.Parse(value);
                }
                catch
                {
                    Console.WriteLine(
                        $"Failed at splitIndex=[{splitIndex}]; value=[{value}]");
                    throw;
                }
            }
        }

        internal static IEnumerable<TestCase> LoadTestCases()
            => LoadInputLinesFromFile().Select(ConvertToTestCase);

        private static IEnumerable<string> LoadInputLinesFromFile()
        {
            // Working directory is something like
            // S:\git\aris-applications\submodules\aris-integration-sdk\common\platform-dotnet\SoundMetrics.Aris.Core.UT\bin\Debug\netcoreapp3.1

            Console.WriteLine($"cwd=[{Directory.GetCurrentDirectory()}]");
            var filePath = Path.Combine(@"..\..\..", InputFileName);

            return ReadTestInputs(filePath);

            static IEnumerable<string> ReadTestInputs(string filePath)
            {
                Assert.IsTrue(File.Exists(filePath));

                string[] nonEmptyLines;

                using (var reader = new StreamReader(filePath))
                {
                    nonEmptyLines = ReadNonEmptyLines(reader).ToArray();
                }

                Assert.IsTrue(nonEmptyLines.Length > 1);

                var firstLine = nonEmptyLines[0];
                Assert.IsTrue(firstLine.StartsWith("System"));

                return nonEmptyLines.Skip(1);
            }

            static IEnumerable<string> ReadNonEmptyLines(TextReader reader)
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrWhiteSpace(line)
                        && !line.StartsWith("#"))
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}
