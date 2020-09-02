using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Device;
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

                    FrameHeader frameHeader;

                    while (true)
                    {
                        if (ReadFrameHeader(file, out frameHeader))
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
                var (_, _, totalSampleCount, _) = Device.SonarConfig.GetSampleGeometry(frameHeader);

                {
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
            }
        }

        public static IEnumerable<Frame> EnumerateFrames(string arisFilePath)
        {
            using (var file = System.IO.File.OpenRead(arisFilePath))
            {
                return EnumerateFrames(file);
            }
        }

        public static IEnumerable<Frame> EnumerateFrames(Stream stream)
        {
            if (ReadFileHeader(stream, out var fileHeader, out var badReason))
            {
                FrameHeader frameHeader;

                while (true)
                {
                    if (ReadFrameHeader(stream, out frameHeader))
                    {
                        var samples = ReadSamples(stream, frameHeader);
                        yield return new Frame(frameHeader, samples);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                throw new Data.FormatException(badReason);
            }

            ByteBuffer ReadSamples(Stream stream, in FrameHeader frameHeader)
            {
                var (_, _, totalSampleCount, _) = SonarConfig.GetSampleGeometry(frameHeader);
                var sampleBuffer =
                    new ByteBuffer(
                        length: totalSampleCount,
                        initializeBuffer: (Span<byte> buffer) =>
                        {
                            var bytesRead = stream.Read(buffer);
                            if (bytesRead != totalSampleCount)
                            {
                                throw new Data.FormatException("Couldn't read all frame samples");
                            }
                        });
                return sampleBuffer;
            }
        }

        public static int CheckFileForProblems()
        {
            throw new NotImplementedException();
        }

        internal static bool ReadFrameHeader(Stream stream, out FrameHeader frameHeader)
        {
            if (stream.ReadStruct(out frameHeader))
            {
                ValidateFrameHeader(frameHeader);
                return true;
            }

            return false;
        }

        private static void AdvancePastFileHeader(Stream stream)
        {
            var fileHeaderSize = Marshal.SizeOf<FileHeader>();
            var pos = stream.Seek(fileHeaderSize, SeekOrigin.Begin);
            if (pos != fileHeaderSize)
            {
                throw new Exception("Incomplete file header");
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
            using (var file = System.IO.File.OpenRead(arisFilePath))
            {
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
        }

        internal static bool ReadFileHeader(
            Stream stream,
            out FileHeader fileHeader,
            out string reason)
        {
            if (stream.ReadStruct(out fileHeader))
            {
                if (fileHeader.Version != FileHeader.ArisFileSignature)
                {
                    fileHeader = default;
                    reason = $"Invalid file signature";
                    return false;
                }

                reason = "";
                return true;
            }
            else
            {
                fileHeader = default;
                reason = $"Couldn't read the file header";
                return false;
            }
        }

        internal static bool IsValidFrameHeader(in FrameHeader frameHeader, out string reason)
        {
            if (frameHeader.Version != FrameHeader.ArisFrameSignature)
            {
                reason = "Invalid frame signature";
                return false;
            }

            var pingMode = Device.PingMode.From((int)frameHeader.PingMode);
            if (!pingMode.IsValid)
            {
                reason = $"Invalid ping mode '{frameHeader.PingMode}'";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
