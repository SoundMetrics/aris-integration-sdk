using CommandLine;

namespace ExtractRecordingInfo
{
    [Verb("orientation", HelpText = "Extracts orientation information from the recording")]
    public sealed class OrientationOptions
    {
        [Value(0, Required = true, MetaName = "Path")]
        public string Path { get; set; }

        /// <summary>
        /// Zero-based frame index.
        /// </summary>
        [Value(1, Required = false, MetaName = "StartIndex")]
        public int StartIndex { get; set; }
    }
}
