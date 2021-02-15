using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.File
{
    public class FileTraits
    {
        public long FileLength { get; private set; }
        public SampleGeometry Geometry { get; private set; }
        public int SerializedFrameSize { get; private set; }
        public int FileHeaderFrameCount { get; private set; }
        public double CalculatedFrameCount { get; private set; }
        public int? ValidFrameHeaderCount { get; private set; }
        public FileIssues Issues { get; private set; }

        public bool HasIssues => Issues != 0;

        public bool HasIssue(FileIssues issue) => ((int)Issues & (int)issue) != 0;

        public IEnumerable<string> IssueDescriptions =>
            FileIssueDescriptions.GetFlagDescriptions(Issues);

        public static bool GetFileTraits(
            string path,
            bool validateFrameHeaders,
            out FileTraits? fileTraits)
        {
            try
            {
                using var stream = System.IO.File.OpenRead(path);
                fileTraits = GetFileTraits(stream, validateFrameHeaders);
                return true;
            }
            catch (FileNotFoundException)
            {
                fileTraits = default;
                return false;
            }
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

                        if (SystemConfiguration.TryGetSampleGeometry(frameHeader, out var geometry))
                        {
                            var serializedFrameSize = geometry.TotalSampleCount + frameHeaderSize;
                            var calculatedFrameCount =
                                (double)(fileSize - fileHeaderSize) / serializedFrameSize;
                            var wholeFrames = Math.Floor(calculatedFrameCount);

                            var validFrameHeaderCount =
                                ValidateFrameHeaders(stream, firstFramePosition, geometry, validateFrameHeaders);

                            var issues = FileIssues.None;
                            issues = wholeFrames == 0 ? issues | FileIssues.NoFrames : issues;
                            issues =
                                validFrameHeaderCount is null
                                    ? issues | FileIssues.InvalidFrameHeaders
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
                                Issues = FileIssues.InvalidFirstFrameHeader,
                            };
                        }
                    }
                    else
                    {
                        return new FileTraits
                        {
                            FileLength = fileSize,
                            Issues = FileIssues.InvalidFirstFrameHeader,
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
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
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
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    // Don't throw from the finally.
                }
            }
        }

        public override string ToString()
        {
            var issues = string.Join("; ", IssueDescriptions);
            return $"FileLength={FileLength:N0}"
                + $"; Geometry=[{Geometry}]"
                + $"; SerializedFrameSize={SerializedFrameSize:N0}"
                + $"; CalculatedFrameCount={CalculatedFrameCount:N0}"
                + $"; ValidFrameHeaderCount={ValidFrameHeaderCount:N0}"
                + $"; Issues='{issues}'";
        }
    }
}
