using SoundMetrics.Aris.Headers;
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

                        while (true)
                        {
                            if (ReadFrameHeader(file, out ArisFrameHeader frameHeader))
                            {
                                AdvancePastSamples(file, frameHeader);
                                yield return FrameResult.FromFrame(frameHeader);
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

            bool ReadFrameHeader(FileStream file, out ArisFrameHeader frameHeader)
            {
                if (Read(file, out frameHeader))
                {
                    ValidateFrameHeader(frameHeader);
                    return true;
                }

                return false;
            }
        }

        private static bool Read<T>(Stream stream, out T t)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var readBuffer = new byte[size];

            var bytesRead = stream.Read(readBuffer);
            if (bytesRead == 0)
            {
                t = default;
                return false;
            }
            else if (bytesRead != size)
            {
                throw new Exception("Couldn't read the expected value");
            }

            var hmem = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(readBuffer, 0, hmem, size);

                t = Marshal.PtrToStructure<T>(hmem);
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(hmem);
            }
        }

        internal static bool IsValidFileHeader(string arisFilePath, out string reason)
        {
            using (var file = System.IO.File.OpenRead(arisFilePath))
            {
                if (Read(file, out ArisFileHeader fileHeader))
                {
                    if (fileHeader.Version != ArisFileHeader.ArisFileSignature)
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
