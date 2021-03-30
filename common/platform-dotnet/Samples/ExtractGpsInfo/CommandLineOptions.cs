using CommandLine;
using System.Collections.Generic;

namespace ExtractGpsInfo
{
    public class CommandLineOptions
    {
        [Option('s', "summarize", Required = false, HelpText = "Summarize the file's GPS info state")]
        public bool Summarize { get; set; }

        [Value(0, Required = true, MetaName = "Paths")]
        public IEnumerable<string> Paths { get; set; }
    }
}
