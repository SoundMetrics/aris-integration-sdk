using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.File;
using System;
using System.IO;

namespace ExtractRecordingInfo
{
    using static ErrorCodes;
    using static Math;

    public static class GPSProcessor
    {
        public static int ProcessGPS(GpsOptions options)
        {
            var console = Console.Out;

            var process =
                options.Summary ? (FileAction)SummarizeGpsInfo : DumpGpsInfo;
            var recordingPath = options.Path;

            console.WriteLine($"Recording=[{recordingPath}]");
            if (File.Exists(recordingPath))
            {
                process(recordingPath, console);
            }
            else
            {
                Console.Error.WriteLine($"File does not exist: [{recordingPath}]");
                return FileNotFound;
            }

            return 0;
        }

        private delegate void FileAction(string recordingPath, TextWriter writer);

        private static void SummarizeGpsInfo(
            string recordingPath,
            TextWriter writer)
        {
            int frameCount = 0;
            int framesWithGpsAge = 0;
            int framesWithMaxGpsAge = 0;
            uint minGpsAge = 0;
            uint maxGpsAge = 0;

            // A `long` is large enough to sum a couple billion frames' GPS ages.
            long cumulativeGpsAges = 0;

            ProcessFrames(recordingPath);
            LogFrames();

            void ProcessFrames(string recordingPath)
            {
                foreach (var frameHeader in
                    ArisRecording.EnumerateFrameHeaders(recordingPath))
                {
                    ProcessFrameHeader(frameHeader);
                }
            }

            void ProcessFrameHeader(in FrameHeader frameHeader)
            {
                var gpsAge = frameHeader.GpsTimeAge;

                framesWithGpsAge += gpsAge != 0 ? 1 : 0;
                framesWithMaxGpsAge += gpsAge == ~0u ? 1 : 0;
                cumulativeGpsAges += gpsAge;

                minGpsAge = Min(minGpsAge, gpsAge);
                maxGpsAge = Max(maxGpsAge, gpsAge);

                ++frameCount;
            }

            void LogFrames()
            {
                writer.WriteLine($"Frame count=[{frameCount}]");

                if (frameCount > 0)
                {
                    writer.WriteLine($"Frames with GPS age=[{framesWithGpsAge}]");
                    writer.WriteLine($"Frames with GPS max age=[{framesWithMaxGpsAge}]");

                    var averageGpsAge = (double)cumulativeGpsAges / frameCount;
                    writer.WriteLine($"Average GPS age=[{(FineDuration)averageGpsAge:F1}]");

                    writer.WriteLine($"Minimum GPS age=[{(FineDuration)minGpsAge}]");
                    writer.WriteLine($"Maximum GPS age=[{(FineDuration)maxGpsAge}]");
                }
            }
        }

        private static void DumpGpsInfo(
            string recordingPath,
            TextWriter writer)
        {
            throw new NotImplementedException(nameof(DumpGpsInfo));
        }
    }
}
