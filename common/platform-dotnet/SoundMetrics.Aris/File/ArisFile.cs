using SoundMetrics.Aris.Data;
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
            return EnumerateAllFrameHeaders(System.IO.File.OpenRead(arisFilePath));

            // This exists only so we can catch and return in the main body of the function.
            IEnumerable<ArisFrameHeader> EnumerateAllFrameHeaders(FileStream file)
            {
                using (file)
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
            throw new NotImplementedException();
        }

        private static bool ReadFrameHeader(FileStream file, in Span<ArisFrameHeader> frameHeader)
        {
            if (Read(file, frameHeader))
            {
                ValidateFrameHeader(MemoryMarshal.GetReference(frameHeader));
                return true;
            }

            return false;
        }

        private static void AdvancePastFileHeader(FileStream file)
        {
            var fileHeaderSize = Marshal.SizeOf<ArisFileHeader>();
            var pos = file.Seek(fileHeaderSize, SeekOrigin.Begin);
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
                var fileHeaderArray = new Memory<ArisFileHeader>(new ArisFileHeader[1]);
                var span = fileHeaderArray.Span;

                if (Read(file, span))
                {
                    if (span[0].Version != ArisFileHeader.ArisFileSignature)
                    {
                        reason = $"Invalid file signature in '{arisFilePath}'";
                        return false;
                    }

                    reason = "";
                    return true;
                }
                else
                {
                    reason = $"Couldn't read the file header in '{arisFilePath}'";
                    return false;
                }
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
