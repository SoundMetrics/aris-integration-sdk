using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Device;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.File
{
    public static partial class ArisFile
    {
        public static long GetFileLength(string filePath) =>
            new FileInfo(filePath).Length;

        public static IEnumerable<ArisFrameHeader> EnumerateFrameHeaders(string arisFilePath)
        {
            using (var file = System.IO.File.OpenRead(arisFilePath))
            {
                if (IsValidFileHeader(arisFilePath, out string badFileHeader))
                {
                    AdvancePastFileHeader(file);

                    var frameHeaderArray = new Memory<ArisFrameHeader>(new ArisFrameHeader[1]);

                    while (true)
                    {
                        var span = frameHeaderArray.Span;
                        if (ReadFrameHeader(file, span))
                        {
                            AdvancePastSamples(file, MemoryMarshal.GetReference(span));
                            yield return MemoryMarshal.GetReference(span);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    throw new ArisFormatException(badFileHeader);
                }
            }

            static void AdvancePastSamples(FileStream file, in ArisFrameHeader frameHeader)
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

        public static IEnumerable<ArisFrame> EnumerateFrames(string arisFilePath)
        {
            using (var file = System.IO.File.OpenRead(arisFilePath))
            {
                return EnumerateFrames(file);
            }
        }

        public static IEnumerable<ArisFrame> EnumerateFrames(Stream stream)
        {
            if (ReadFileHeader(stream, out var fileHeader, out var badReason))
            {
                var frameHeaderArray = new Memory<ArisFrameHeader>(new ArisFrameHeader[1]);

                while (true)
                {
                    var span = frameHeaderArray.Span;
                    if (ReadFrameHeader(stream, span))
                    {
                        var samples = ReadSamples(stream, frameHeaderArray.Span[0]);
                        yield return new ArisFrame(frameHeaderArray.Span[0], samples);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                throw new ArisFormatException(badReason);
            }

            ByteBuffer ReadSamples(Stream stream, in ArisFrameHeader frameHeader)
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
                                throw new ArisFormatException("Couldn't read all frame samples");
                            }
                        });
                return sampleBuffer;
            }
        }

        private static bool ReadFrameHeader(Stream stream, in Span<ArisFrameHeader> frameHeader)
        {
            if (Read(stream, frameHeader))
            {
                ValidateFrameHeader(MemoryMarshal.GetReference(frameHeader));
                return true;
            }

            return false;
        }

        private static void AdvancePastFileHeader(Stream stream)
        {
            var fileHeaderSize = Marshal.SizeOf<ArisFileHeader>();
            var pos = stream.Seek(fileHeaderSize, SeekOrigin.Begin);
            if (pos != fileHeaderSize)
            {
                throw new Exception("Incomplete file header");
            }
        }

        private static void ValidateFrameHeader(in ArisFrameHeader frameHeader)
        {
            if (!IsValidFrameHeader(frameHeader, out string reason))
            {
                throw new Exception($"Invalid frame header: {reason}");
            }
        }

        private static bool Read<T>(Stream stream, in Span<T> t)
            where T : struct
        {
            var byteSpan = MemoryMarshal.AsBytes(t);
            var bytesRead = stream.Read(byteSpan);
            return bytesRead == byteSpan.Length;
        }

        internal static bool IsValidFileHeader(string arisFilePath, out string reason)
        {
            using (var file = System.IO.File.OpenRead(arisFilePath))
            {
                if (ReadFileHeader(file, out var fileHeader, out var badReason))
                {
                    if (fileHeader.Version != ArisFileHeader.ArisFileSignature)
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
            out ArisFileHeader fileHeader,
            out string reason)
        {
            var fileHeaderArray = new Memory<ArisFileHeader>(new ArisFileHeader[1]);
            var span = fileHeaderArray.Span;

            if (Read(stream, span))
            {
                if (span[0].Version != ArisFileHeader.ArisFileSignature)
                {
                    fileHeader = default;
                    reason = $"Invalid file signature";
                    return false;
                }

                fileHeader = span[0];
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

        internal static bool IsValidFrameHeader(in ArisFrameHeader frameHeader, out string reason)
        {
            if (frameHeader.Version != ArisFrameHeader.ArisFrameSignature)
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
