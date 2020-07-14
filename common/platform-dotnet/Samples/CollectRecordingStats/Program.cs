using CommandLine;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace CollectRecordingStats
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(options =>
                    LogProgramTime(() => RunProgram(options)))
                .WithNotParsed(errors => { });
        }

        private static void RunProgram(CommandLineOptions options)
        {
            var result = Collection.GatherAllStats(options.FolderPaths.ToArray());

            var columnNames = Extractors.Select(v => v.Name);
            Console.WriteLine(string.Join(",", columnNames));

            foreach (var file in result.Files)
            {
                if (file.ErrorMessage.Length > 0)
                {
                    //Console.Error.WriteLine($"oopsie: '{file.FailureMessages[0]}': '{file.Path}'");
                }
                else
                {
                    Console.WriteLine(FormatFileOutput(file));
                }
            }
        }

        private static void LogProgramTime(Action action)
        {
            var success = false;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                action();
                success = true;
            }
            finally
            {
                var elapsed = stopwatch.Elapsed;
                var result = success ? "succeeded" : "failed";
                Console.WriteLine($"Program {result}; elapsed time=[{elapsed}]");
            }
        }

        private delegate string ExtractValue(CollectedFile collectedFile);

        private static readonly (string Name, ExtractValue Extract)[] Extractors =
        {
            ("FileLength", file => InvariantString(file.FileLength)),
            ("FrameCount", file => InvariantString(file.FrameCount)),

            ("FirstFrameSonarTimestamp", file => Quote(file.FirstFrameSonarTimestamp)),
            ("LastFrameSonarTimestamp", file => Quote(file.LastFrameSonarTimestamp)),

            ("AverageFramePeriodMillis",
                 file => file.AverageFramePeriod?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)), // sonar timestamp

            ("FirstGoTime", file => file.FirstGoTime?.ToString(CultureInfo.InvariantCulture)),
            ("LastGoTime", file => file.LastGoTime?.ToString(CultureInfo.InvariantCulture)),
            ("AverageGoTimePeriodMillis", file =>
                (file.AverageGoTimePeriodMicros / 1000.0)?.ToString(CultureInfo.InvariantCulture)),

            ("FilePath", file => Quote(file.Path)),
        };

        private static string FormatFileOutput(CollectedFile file) =>
            string.Join(",", Extractors.Select(ex => ex.Extract(file)));

        private static string InvariantString(long i) => i.ToString(CultureInfo.InvariantCulture);

        private static string Quote(string s) => '"' + s + '"';

        private static string Quote(DateTime? timestamp) =>
            '"' + timestamp?.ToString(CultureInfo.InvariantCulture) + '"';

        private static string Quote(TimeSpan? timespan) =>
            '"' + timespan?.ToString(null, CultureInfo.InvariantCulture) + '"';
    }
}
