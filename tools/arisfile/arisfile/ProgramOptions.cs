using CommandLine;

namespace arisfile
{
    class ProgramOptions
    {
        [Option('f', "file-path", Required = true, HelpText = "Path to the ARIS recording to be analyzed.")]
        public string FilePath { get; set; } = "";
    }
}
