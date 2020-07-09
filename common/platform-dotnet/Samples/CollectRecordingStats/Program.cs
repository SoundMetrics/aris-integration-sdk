using CommandLine;
using System;
using System.Globalization;
using System.Linq;

namespace CollectRecordingStats
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
            var result = Collection.GatherAllStats(options.FolderPaths.ToArray());

            Console.WriteLine(string.Join(",", ColumnNames));

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

        private static readonly string[] ColumnNames =
        {
            "FileLength",
            "FrameCount",

            "FirstFrameSonarTimestamp",
            "LastFrameSonarTimestamp",
            "AverageFramePeriodMillis", // sonar timestamp

            "FirstGoTime",
            "LastGoTime",
            "AverageGoTimePeriodMicros",

            "FilePath",
        };

        private static string FormatFileOutput(CollectionFile file) =>
            string.Join(",",
                new string[]
                {
                    InvariantString(file.FileLength),
                    InvariantString(file.FrameCount),
                    Quote(file.FirstFrameSonarTimestamp),
                    Quote(file.LastFrameSonarTimestamp),
                    file.AverageFramePeriod?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),

                    file.FirstGoTime?.ToString(CultureInfo.InvariantCulture),
                    file.LastGoTime?.ToString(CultureInfo.InvariantCulture),
                    file.AverageGoTimePeriodMicros?.ToString(CultureInfo.InvariantCulture),

                    Quote(file.Path),
                });

        private static string InvariantString(long i) => i.ToString(CultureInfo.InvariantCulture);

        private static string Quote(string s) => '"' + s + '"';

        private static string Quote(DateTime? timestamp) =>
            '"' + timestamp?.ToString(CultureInfo.InvariantCulture) + '"';

        private static string Quote(TimeSpan? timespan) =>
            '"' + timespan?.ToString(null, CultureInfo.InvariantCulture) + '"';
    }
}
