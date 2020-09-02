using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Device;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.File
{
    public class FileTraits
    {
        public long FileLength;
        public SampleGeometry Geometry;
        public int SerializedFrameSize;
        public int FileHeaderFrameCount;
        public double CalculatedFrameCount;
        public FileIssue Issues;

        public bool HasIssues => Issues != 0;

        public bool HasIssue(FileIssue issue) => ((int)Issues & (int)issue) != 0;

        public IEnumerable<string> IssueDescriptions =>
            FileIssueDescriptions.GetFlagDescriptions(Issues);

        public static FileTraits GetFileTraits(string path)
        {
            using var stream = System.IO.File.OpenRead(path);
            return GetFileTraits(stream);
        }

        private static FileTraits GetFileTraits(FileStream stream)
        {
            var startingPosition = stream.Position;
            stream.Position = 0;

            try
            {
                var fileSize = stream.Length;

                if (ArisRecording.ReadFileHeader(
                        stream,
                        out var fileHeader,
                        out var issue))
                {
                    if (ArisRecording.ReadFrameHeader(
                        stream,
                        out var frameHeader))
                    {
                        var fileHeaderSize = Marshal.SizeOf<FileHeader>();
                        var frameHeaderSize = Marshal.SizeOf<FrameHeader>();

                        var geometry = SonarConfig.GetSampleGeometry(frameHeader);
                        var serializedFrameSize = geometry.TotalSampleCount + frameHeaderSize;
                        var calculatedFrameCount =
                            (double)(fileSize - fileHeaderSize) / serializedFrameSize;
                        var wholeFrames = Math.Floor(calculatedFrameCount);

                        var issues =
                            wholeFrames == 0 ? FileIssue.NoFrames : FileIssue.None;

                        return new FileTraits
                        {
                            FileLength = fileSize,
                            Geometry = geometry,
                            SerializedFrameSize = serializedFrameSize,
                            FileHeaderFrameCount = (int)fileHeader.FrameCount,
                            CalculatedFrameCount = calculatedFrameCount,
                            Issues = issues,
                        };
                    }
                    else
                    {
                        return new FileTraits
                        {
                            FileLength = fileSize,
                            Issues = FileIssue.InvalidFirstFrameHeader,
                        };
                    }
                }
                else
                {
                    return new FileTraits
                    {
                        FileLength = fileSize,
                        Issues = issue,
                    };
                }
            }
            finally
            {
                try
                {
                    stream.Position = startingPosition;
                }
                catch
                {
                    // Don't throw from the finally.
                }
            }
        }
    }
}
