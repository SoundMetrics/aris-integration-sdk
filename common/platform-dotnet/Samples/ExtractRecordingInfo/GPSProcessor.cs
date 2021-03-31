using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.File;
using System;
using System.IO;

namespace ExtractRecordingInfo
{
    using static ErrorCodes;

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
                framesWithGpsAge += frameHeader.GpsTimeAge != 0 ? 1 : 0;
                framesWithMaxGpsAge += frameHeader.GpsTimeAge == ~0u ? 1 : 0;
                ++frameCount;
            }

            void LogFrames()
            {
                writer.WriteLine($"Frame count=[{frameCount}]");
                writer.WriteLine($"Frames with GPS age=[{framesWithGpsAge}]");
                writer.WriteLine($"Frames with GPS max age=[{framesWithMaxGpsAge}]");
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
