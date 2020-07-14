﻿using SoundMetrics.Aris.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.File
{
    public static class ArisFile
    {
        public class FrameResult
        {
            public ArisFrameHeader FrameHeader;
            public bool Success;
            public string ErrorMessage;

            public static FrameResult FromFrame(in ArisFrameHeader frameHeader)
            {
                return new FrameResult
                {
                    Success = true,
                    FrameHeader = frameHeader,
                    ErrorMessage = "",
                };
            }

            public static FrameResult FromError(string errorMessage)
            {
                return new FrameResult
                {
                    Success = false,
                    ErrorMessage = errorMessage ?? "",
                };
            }
        }

        public static long GetFileLength(string filePath) =>
            new FileInfo(filePath).Length;

        public static IEnumerable<FrameResult> EnumerateFrameHeaders(string arisFilePath)
        {
            try
            {
                return EnumerateAllFrameHeaders(OpenFile());
            }
            catch (IOException ex)
            {
                return new[]
                {
                    FrameResult.FromError($"An exception occurred: {ex.Message}")
                };
            }

            // This exists only so we can catch and return in the main body of the function.
            IEnumerable<FrameResult> EnumerateAllFrameHeaders(FileStream file)
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
                                yield return FrameResult.FromFrame(MemoryMarshal.GetReference(span));
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        yield return FrameResult.FromError(badFileHeader);
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

            FileStream OpenFile()
            {
                // TODO handle IOException here on currently active file...
                return System.IO.File.OpenRead(arisFilePath);
            }

            void ValidateFrameHeader(in ArisFrameHeader frameHeader)
            {
                if (!IsValidFrameHeader(frameHeader, out string reason))
                {
                    throw new Exception($"Invalid frame header: {reason}");
                }
            }

            void AdvancePastFileHeader(FileStream file)
            {
                var fileHeaderSize = Marshal.SizeOf<ArisFileHeader>();
                var pos = file.Seek(fileHeaderSize, SeekOrigin.Begin);
                if (pos != fileHeaderSize)
                {
                    throw new Exception("Incomplete file header");
                }
            }

            bool ReadFrameHeader(FileStream file, in Span<ArisFrameHeader> frameHeader)
            {
                if (Read(file, frameHeader))
                {
                    ValidateFrameHeader(MemoryMarshal.GetReference(frameHeader));
                    return true;
                }

                return false;
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
                        reason = "Invalid file signature";
                        return false;
                    }

                    reason = "";
                    return true;
                }
                else
                {
                    reason = "Couldn't read the file header";
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
