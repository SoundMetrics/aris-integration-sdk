using CommandLine;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.File;
using System;
using System.Diagnostics;
using System.IO;

namespace ExtractGpsInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(RunProgram)
                .WithNotParsed(errors => { });
        }

        private static void RunProgram(CommandLineOptions options)
        {
            var console = Console.Out;

            console.WriteLine("Starting {0}", Process.GetCurrentProcess().ProcessName);

            var process =
                options.Summarize ? (FileAction)SummarizeGpsInfo : DumpGpsInfo;

            foreach (var recordingPath in options.Paths)
            {
                console.WriteLine($"Recording=[{recordingPath}]");
                if (File.Exists(recordingPath))
                {
                    process(recordingPath, console);
                }
                else
                {
                    Console.Error.WriteLine($"File does not exist: [{recordingPath}]");
                }
            }
        }

        private delegate void FileAction(string recordingPath, TextWriter writer);

        private static void SummarizeGpsInfo(
            string recordingPath,
            TextWriter writer)
        {
            var frameCount = 0;
            var framesWithGpsAge = 0;
            var framesWithMaxGpsAge = 0;

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
