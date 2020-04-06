using arisfile.analysis;
using CommandLine;
using Serilog;
using System.IO;

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
        }
    }
}
