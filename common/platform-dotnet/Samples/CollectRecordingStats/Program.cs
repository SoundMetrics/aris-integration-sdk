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

            var columnNames = Extractors.Select(ex => ex.ValueName);
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

        private struct ValueExtractor
        {
            public string ValueName;
            public Func<CollectionFile, string> Extract;

            public ValueExtractor(string valueName, Func<CollectionFile, string> extract)
            {
                this.ValueName = valueName;
                this.Extract = extract;
            }
        }

        private static readonly ValueExtractor[] Extractors =
        {
            new ValueExtractor("FileLength", file => InvariantString(file.FileLength)),
            new ValueExtractor("FrameCount", file => InvariantString(file.FrameCount)),

            new ValueExtractor("FirstFrameSonarTimestamp", file => Quote(file.FirstFrameSonarTimestamp)),
            new ValueExtractor("LastFrameSonarTimestamp", file => Quote(file.LastFrameSonarTimestamp)),

            new ValueExtractor("AverageFramePeriodMillis",
                 file => file.AverageFramePeriod?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)), // sonar timestamp

            new ValueExtractor("FirstGoTime", file => file.FirstGoTime?.ToString(CultureInfo.InvariantCulture)),
            new ValueExtractor("LastGoTime", file => file.LastGoTime?.ToString(CultureInfo.InvariantCulture)),
            new ValueExtractor("AverageGoTimePeriodMillis", file =>
                (file.AverageGoTimePeriodMicros / 1000.0)?.ToString(CultureInfo.InvariantCulture)),

            new ValueExtractor("FilePath", file => Quote(file.Path)),
        };

        private static string FormatFileOutput(CollectionFile file) =>
            string.Join(",", Extractors.Select(ex => ex.Extract(file)));

        private static string InvariantString(long i) => i.ToString(CultureInfo.InvariantCulture);

        private static string Quote(string s) => '"' + s + '"';

        private static string Quote(DateTime? timestamp) =>
            '"' + timestamp?.ToString(CultureInfo.InvariantCulture) + '"';

        private static string Quote(TimeSpan? timespan) =>
            '"' + timespan?.ToString(null, CultureInfo.InvariantCulture) + '"';
    }
}
