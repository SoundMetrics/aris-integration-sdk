using CommandLine;
using System.Collections.Generic;

namespace CollectRecordingStats
{
    public sealed class CommandLineOptions
    {
        [Option('l', "log", Required = false, HelpText = "Log path.")]
        public string LogPath { get; set; }

        [Value(0, Required = true, MetaName = "Paths")]
        public IEnumerable<string> FolderPaths { get; set; }
    }
}
