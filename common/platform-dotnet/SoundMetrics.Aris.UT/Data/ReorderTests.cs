using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Data;
using System;
using System.IO;

namespace SoundMetrics.Aris.UT
{
    [TestClass]
    public class ReorderTests
    {
        [TestMethod]
        public void ReorderTest()
        {
            int testCaseIndex = 0;
            foreach (var testCase in FileTestCases)
            {
                var index = testCaseIndex++;
                var testDescription = $"Test case {index}";

                Console.WriteLine(testDescription);

                var inputSamples = LoadSampleFile(testCase.InputSampleFile);
                var expectedSamples = LoadSampleFile(testCase.ExpectedSampleFile);

                Console.WriteLine($"Input file: {testCase.InputSampleFile}");
                Console.WriteLine($"Expected file: {testCase.ExpectedSampleFile}");

                var inputFrame =
                    CreateTestFrame(
                        testCase.PingMode,
                        testCase.SamplesPerBeam,
                        inputSamples);

                var outputFrame = Reorder.ReorderFrame(inputFrame);

                Assert.IsTrue(
                    AreEqual(expectedSamples.Span, outputFrame.Samples),
                    testDescription);

                var secondTime = Reorder.ReorderFrame(outputFrame);
                Assert.IsTrue(
                    AreEqual(expectedSamples.Span, secondTime.Samples),
                    testDescription + " (second time)");
                Assert.AreSame(outputFrame, secondTime);
            }
        }

        private static Frame CreateTestFrame(
            int pingMode,
            int samplesPerBeam,
            ReadOnlyMemory<byte> samples)
        {
            var frameHeader = new FrameHeader
            {
                PingMode = (uint)pingMode,
                SamplesPerBeam = (uint)samplesPerBeam,
            };

            return new Frame(frameHeader, new ByteBuffer(samples));
        }

        private static bool AreEqual(ReadOnlySpan<byte> a, ByteBuffer b)
        {
            if (a.Length != b.Length)
            {
                Console.WriteLine("Lengths are not equal");
                return false;
            }

            var result = a.SequenceEqual(b.Span);
            Console.WriteLine($"AreEqual({a.Length} bytes) => {result}");
            return result;
        }

        private struct FileTestCase
        {
            public string InputSampleFile;
            public string ExpectedSampleFile;
            public int PingMode;
            public int SamplesPerBeam;
        }

        private static readonly FileTestCase[] FileTestCases = new[]
        {
            new FileTestCase
            {
                InputSampleFile = "input_pingmode_1_1200.dat",
                ExpectedSampleFile = "expected_pingmode_1_1200.dat",
                PingMode = 1,
                SamplesPerBeam = 512,
            },

            new FileTestCase
            {
                InputSampleFile = "input_pingmode_1_1800.dat",
                ExpectedSampleFile = "expected_pingmode_1_1800.dat",
                PingMode = 1,
                SamplesPerBeam = 544,
            },

            new FileTestCase
            {
                InputSampleFile = "input_pingmode_3.dat",
                ExpectedSampleFile = "expected_pingmode_3.dat",
                PingMode = 3,
                SamplesPerBeam = 512,
            },

            new FileTestCase
            {
                InputSampleFile = "input_pingmode_6.dat",
                ExpectedSampleFile = "expected_pingmode_6.dat",
                PingMode = 6,
                SamplesPerBeam = 410,
            },

            new FileTestCase
            {
                InputSampleFile = "input_pingmode_9.dat",
                ExpectedSampleFile = "expected_pingmode_9.dat",
                PingMode = 9,
                SamplesPerBeam = 512,
            }
        };

        private static ReadOnlyMemory<byte> LoadSampleFile(string path)
        {
            const string prefixPath = @"Data\LinkedDataFiles";
            string filePath = Path.Combine(prefixPath, path);

            using (var file = System.IO.File.OpenRead(filePath))
            {
                var buffer = new byte[file.Length];
                if (file.Read(buffer) != buffer.Length)
                {
                    throw new Exception($"Failed reading file '{filePath}'");
                }

                return buffer;
            }
        }
    }
}
