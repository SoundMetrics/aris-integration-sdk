using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.File
{
    using static Serialization;

    public static partial class ArisRecording
    {
        public static long GetFileLength(string filePath) =>
            new FileInfo(filePath).Length;

        public static IEnumerable<FrameHeader> EnumerateFrameHeaders(string arisFilePath)
        {
            using (var file = System.IO.File.OpenRead(arisFilePath))
            {
                if (IsValidFileHeader(arisFilePath, out string badFileHeader))
                {
                    AdvancePastFileHeader(file);

                    while (true)
                    {
                        if (ReadFrameHeaderWithValidation(file, out var frameHeader))
                        {
                            AdvancePastSamples(file, frameHeader);
                            yield return frameHeader;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    throw new Data.FormatException(badFileHeader);
                }
            }

            static void AdvancePastSamples(FileStream file, in FrameHeader frameHeader)
            {
                if (SystemConfiguration.TryGetSampleGeometry(frameHeader, out var sampleGeometry))
                {
                    var totalSampleCount = sampleGeometry.TotalSampleCount;
                    var pos = file.Position;

                    if (file.Seek(totalSampleCount, SeekOrigin.Current) == pos + totalSampleCount)
                    {
                        // Successful.
                    }
                    else
                    {
                        // Couldn't seek past the samples, meaning we hit the end
                        // of the file. Ignore this condition and let the next frame
                        // header read fail to get a frame header.
                    }
                }
                else
                {
                    throw new Exception("Couldn't determine sample geometry");
                }
            }
        }

        public static IEnumerable<Frame> EnumerateFrames(string arisFilePath)
        {
            using var file = System.IO.File.OpenRead(arisFilePath);
            return EnumerateFrames(file);
        }

        public static IEnumerable<Frame> EnumerateFrames(FileStream stream)
        {
            if (ReadFileHeader(stream, out var fileHeader, out var issue))
            {
                while (true)
                {
                    if (ReadFrameHeaderWithValidation(stream, out var frameHeader)
#pragma warning disable CA2000 // Dispose objects before losing scope
                        && TryReadSamples(stream, frameHeader, out var samples)
#pragma warning restore CA2000 // Dispose objects before losing scope
                        && !(samples is null)
                        && Frame.TryCreate(frameHeader, samples, out var frame)
                        && !(frame is null))
                    {
                        yield return frame;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                throw new Data.FormatException(FileIssueDescriptions.GetFlagDescription(issue));
            }

            bool TryReadSamples(Stream stream, in FrameHeader frameHeader, out ByteBuffer? samples)
            {
                if (SystemConfiguration.TryGetSampleGeometry(frameHeader, out var sampleGeometry))
                {
                    samples =
                        new ByteBuffer(
                            length: sampleGeometry.TotalSampleCount,
                            initializeBuffer: (Span<byte> buffer) =>
                            {
                                var bytesRead = stream.Read(buffer);
                                if (bytesRead != sampleGeometry.TotalSampleCount)
                                {
                                    throw new Data.FormatException("Couldn't read all frame samples");
                                }
                            });
                    return true;
                }
                else
                {
                    samples = default;
                    return false;
                }
            }
        }

        public static FileTraits CheckFileForProblems()
        {
            throw new NotImplementedException();
        }

        internal static bool ReadFrameHeaderWithValidation(Stream stream, out FrameHeader frameHeader)
        {
            if (ReadFrameHeaderRaw(stream, out frameHeader))
            {
                ValidateFrameHeader(frameHeader);
                return true;
            }

            return false;
        }

        internal static bool ReadFrameHeaderRaw(Stream stream, out FrameHeader frameHeader)
        {
            return stream.ReadStruct(out frameHeader);
        }

        private static void AdvancePastFileHeader(Stream stream)
        {
            var fileHeaderSize = Marshal.SizeOf<FileHeader>();
            var pos = stream.Seek(fileHeaderSize, SeekOrigin.Begin);
            if (pos != fileHeaderSize)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new Exception("Incomplete file header");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
        }

        private static void ValidateFrameHeader(in FrameHeader frameHeader)
        {
            if (!IsValidFrameHeader(frameHeader, out string reason))
            {
                throw new Exception($"Invalid frame header: {reason}");
            }
        }

        internal static bool IsValidFileHeader(string arisFilePath, out string reason)
        {
            using var file = System.IO.File.OpenRead(arisFilePath);
            if (ReadFileHeader(file, out var fileHeader, out var badReason))
            {
                if (fileHeader.Version != FileHeader.ArisFileSignature)
                {
                    reason = $"Invalid file signature in '{arisFilePath}'";
                    return false;
                }

                reason = "";
                return true;
            }
            else
            {
                reason = $"{badReason} in '{arisFilePath}'";
                return false;
            }
        }

        public static bool ReadFileHeader(
            string path,
            out FileHeader fileHeader,
            out FileIssues issue)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            using var stream = System.IO.File.OpenRead(path);
            return ReadFileHeader(stream, out fileHeader, out issue);
        }

        public static bool ReadFileHeader(
            FileStream stream,
            out FileHeader fileHeader,
            out FileIssues issue)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            if (stream.Length == 0)
            {
                fileHeader = default;
                issue = FileIssues.EmptyFile;
                return false;
            }
            else if (stream.ReadStruct(out fileHeader))
            {
                if (fileHeader.Version != FileHeader.ArisFileSignature)
                {
                    fileHeader = default;
                    issue = FileIssues.InvalidFileHeader;
                    return false;
                }

                issue = FileIssues.None;
                return true;
            }
            else
            {
                fileHeader = default;
                issue = FileIssues.IncompleteFileHeader;
                return false;
            }
        }

        internal static bool IsValidFrameHeader(in FrameHeader frameHeader, out string reason)
        {
            if (frameHeader.SonarSerialNumber < 1)
            {
                reason = "Invalid serial number";
                return false;
            }

            if (frameHeader.Version != FrameHeader.ArisFrameSignature)
            {
                reason = "Invalid frame signature";
                return false;
            }

            if (PingMode.TryGet((int)frameHeader.PingMode, out var pingMode))
            {
                reason = "";
                return true;
            }
            else
            {
                reason = $"Invalid ping mode '{frameHeader.PingMode}'";
                return false;
            }
        }
    }
}
