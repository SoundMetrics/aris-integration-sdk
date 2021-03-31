using CommandLine;

namespace ExtractRecordingInfo
{
    [Verb("gps", HelpText = "Extracts GPS information from the recording")]
    public class GpsOptions
    {
        [Option('s', "summary", Required = false, HelpText = "Summarize the file's GPS info state")]
        public bool Summary { get; set; }

        [Value(0, Required = true, MetaName = "Path")]
        public string Path { get; set; }
    }

    [Verb("depth", HelpText = "Extracts depth information from the recording")]
    public class DepthOptions
    {
    }
}
