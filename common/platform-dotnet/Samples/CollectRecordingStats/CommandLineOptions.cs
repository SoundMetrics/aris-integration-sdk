using CommandLine;
using System.Collections.Generic;

namespace CollectRecordingStats
{
    public sealed class CommandLineOptions
    {
        [Value(0, Required = true, MetaName = "Paths")]
        public IEnumerable<string> FolderPaths { get; set; }
    }
}
