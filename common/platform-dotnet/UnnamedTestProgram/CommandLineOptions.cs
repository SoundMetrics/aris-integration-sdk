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
        public TestOptions()
        {
            // Solely for non-nullable properties.
            SerialNumber = "";
            Settings = "";
        }

        [Option("duration", Required = true, HelpText = "The duration of the test, in minutes")]
        public uint Duration { get; set; }

        [Option("serial-number", Required = true, HelpText = "Target ARIS serial number")]
        public string SerialNumber { get; set; }

        [Option("settings", Required = true, HelpText = "Settings to be sent to the sonar")]
        public string Settings { get; set; }
    }
}
