using arisfile.analysis;
using CommandLine;
using Serilog;
using System;
using System.IO;
using static arisfile.analysis.Analysis;

namespace arisfile
{
    static class Program
    {
        static void Main(string[] args)
        {
            const string LoggingTemplate =
                @"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            Log.Logger =
                new LoggerConfiguration()
#if DEBUG
                    .MinimumLevel.Debug()
#endif
                    .WriteTo.Console(outputTemplate: LoggingTemplate)
                    .CreateLogger();

            try
            {
                PerformAnalysis(args);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void PerformAnalysis(string[] args)
        {
            Log.Information("arisfile - ARIS recorded file analysis");

            Parser.Default.ParseArguments<ProgramOptions>(args)
                .WithParsed<ProgramOptions>(options =>
                {
                    try
                    {
                        using (var stream = OpenArisFile(options.FilePath))
                        {
                            Log.Information("Opened '{FilePath}'.", options.FilePath);

                            Analysis.Run(
                                stream
                                , AnalysisFunctions.EmitFrameIndex
                                , AnalysisFunctions.MakeTimelineChecker2()
                                );
                        }
                    }
                    catch (IOException ex)
                    {
                        Log.Error(ex, "An error occurred.");
                    }
                });
        }

        private static Stream OpenArisFile(string path) => File.OpenRead(path);

        private static class AnalysisFunctions
        {
            public static void EmitFrameIndex(ArisFrameAccessor frame)
            {
                Log.Information(
                    $"Frame {frame.ArisFrameHeader.FrameIndex} (calculated: {frame.CalculatedFrameIndex})");
            }

            public static FrameAnalysisFunction MakeTimelineChecker()
            {
                DateTime? previousTopsideTimestamp = null;
                DateTime? previousSonarTimestamp = null;

                return frameAccessor =>
                {
                    var currentTopside = frameAccessor.ArisFrameHeader.GetTopsideTimestamp();
                    var currentSonar = frameAccessor.ArisFrameHeader.GetArisTimestamp();

                    if (previousTopsideTimestamp is DateTime previousTopside
                        && previousSonarTimestamp is DateTime previousSonar)
                    {
                        if (currentTopside < previousTopside || currentSonar < previousSonar)
                        {
                            Log.Warning($"Retrograde timestamp:");
                        }
                    }

                    previousTopsideTimestamp = frameAccessor.ArisFrameHeader.GetTopsideTimestamp();
                    previousSonarTimestamp = frameAccessor.ArisFrameHeader.GetArisTimestamp();

                    Log.Information($"### ARIS: {previousSonarTimestamp}; topside: {previousTopsideTimestamp}");
                };
            }

            public static FrameAnalysisFunction MakeTimelineChecker2()
            {
                ulong? previousTopsideTimestamp = null;
                ulong? previousSonarTimestamp = null;

                return frameAccessor =>
                {
                    var currentTopside = frameAccessor.ArisFrameHeader.FrameTime;
                    var currentSonar = frameAccessor.ArisFrameHeader.sonarTimeStamp;

                    if (previousTopsideTimestamp is ulong previousTopside
                        && previousSonarTimestamp is ulong previousSonar)
                    {
                        if (currentTopside < previousTopside || currentSonar < previousSonar)
                        {
                            Log.Warning("Out-of-order timestamp(s):");
                        }
                    }

                    long? topsideDelta = (long)currentTopside - (long?)previousTopsideTimestamp;
                    long? sonarDelta = (long)currentSonar - (long?)previousSonarTimestamp;

                    previousTopsideTimestamp = frameAccessor.ArisFrameHeader.FrameTime;
                    previousSonarTimestamp = frameAccessor.ArisFrameHeader.sonarTimeStamp;

                    Log.Information(
                        $"    ARIS: {previousSonarTimestamp} (\u03b4 {sonarDelta} \u00b5s); "
                        + $"topside: {previousTopsideTimestamp} (\u03b4 {topsideDelta} \u00b5s)");
                };
            }
        }
    }
}
