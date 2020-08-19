using CommandLine;

namespace UnnamedTestProgram
{
    [Verb("run-script", HelpText = "Runs the specified script file")]
    public sealed class ScriptOptions
    {
        [Option("script-path", HelpText = "Path of the script file")]
        public string? ScriptPath { get; set; }
    }

    [Verb("test", HelpText = "Runs a test")]
    public sealed class TestOptions
    {
        [Option("duration", HelpText = "The duration of the test, in minutes")]
        public uint? Duration { get; set; }

        [Option("serial-number", HelpText = "Target ARIS serial number")]
        public string? SerialNumber { get; set; }
    }
}
