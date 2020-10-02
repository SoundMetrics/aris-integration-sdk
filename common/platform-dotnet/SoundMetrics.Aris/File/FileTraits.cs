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
        public int? ValidFrameHeaderCount;
        public FileIssue Issues;

        public bool HasIssues => Issues != 0;

        public bool HasIssue(FileIssue issue) => ((int)Issues & (int)issue) != 0;

        public IEnumerable<string> IssueDescriptions =>
            FileIssueDescriptions.GetFlagDescriptions(Issues);

        public static FileTraits GetFileTraits(string path, bool validateFrameHeaders)
        {
            using var stream = System.IO.File.OpenRead(path);
            return GetFileTraits(stream, validateFrameHeaders);
        }

        private static FileTraits GetFileTraits(FileStream stream, bool validateFrameHeaders)
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
                    var firstFramePosition = stream.Position;

                    if (ArisRecording.ReadFrameHeaderWithValidation(
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

                        var validFrameHeaderCount =
                            ValidateFrameHeaders(stream, firstFramePosition, geometry, validateFrameHeaders);

                        var issues = FileIssue.None;
                        issues = wholeFrames == 0 ? issues | FileIssue.NoFrames : issues;
                        issues =
                            validFrameHeaderCount is null
                                ? issues | FileIssue.InvalidFrameHeaders
                                : issues;

                        return new FileTraits
                        {
                            FileLength = fileSize,
                            Geometry = geometry,
                            SerializedFrameSize = serializedFrameSize,
                            FileHeaderFrameCount = (int)fileHeader.FrameCount,
                            CalculatedFrameCount = calculatedFrameCount,
                            ValidFrameHeaderCount = validFrameHeaderCount,
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

        private static int? ValidateFrameHeaders(
            FileStream stream,
            long frameStartPosition,
            SampleGeometry geometry,
            bool enabled)
        {
            if (!enabled)
            {
                return null;
            }

            var originalPosition = stream.Position;
            try
            {
                int count = 0;

                stream.Position = frameStartPosition;

                while (ArisRecording.ReadFrameHeaderRaw(stream, out var frameHeader))
                {
                    if (ArisRecording.IsValidFrameHeader(frameHeader, out var _))
                    {
                        ++count;
                        stream.Seek(geometry.TotalSampleCount, SeekOrigin.Current);
                    }
                    else
                    {
                        break;
                    }
                }

                return count;
            }
            finally
            {
                try
                {
                    stream.Position = originalPosition;
                }
                catch
                {
                    // Don't throw from the finally.
                }
            }
        }

        public override string ToString()
        {
            var issues = string.Join("; ", IssueDescriptions);
            return $"FileLength={FileLength}"
                + $"; Geometry=[{Geometry}]"
                + $"; SerializedFrameSize={SerializedFrameSize}"
                + $"; CalculatedFrameCount={CalculatedFrameCount}"
                + $"; ValidFrameHeaderCount={ValidFrameHeaderCount}"
                + $"; Issues='{issues}'";
        }
    }
}
