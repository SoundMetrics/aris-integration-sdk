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

        // NOTE: currently does not return frame data.
        public static IEnumerable<FrameResult> EnumerateFrames(string arisFilePath)
        {
            try
            {
                return EnumerateTheFile(OpenFile());
            }
            catch (IOException ex)
            {
                return new[]
                {
                    FrameResult.FromError($"An exception occurred: {ex.Message}")
                };
            }

            // This exists only so we can catch and return in the main body of the function.
            IEnumerable<FrameResult> EnumerateTheFile(FileStream file)
            {
                using (file)
                {
                    ValidateFileHeader();
                    AdvancePastFileHeader(file);

                    while (true)
                    {
                        var (successfulRead, frameHeader) = ReadFrameHeader(file);
                        if (!successfulRead)
                        {
                            break;
                        }

                        var (_, _, totalSampleCount, _) = Device.SonarConfig.GetSampleGeometry(frameHeader);

                        // TODO not expected behavior, skipping over samples at the moment.
                        {
                            var pos = file.Position;
                            if (file.Seek(totalSampleCount, SeekOrigin.Current) != pos + totalSampleCount)
                            {
                                throw new Exception("Couldn't seek past samples");
                            }
                        }

                        yield return FrameResult.FromFrame(frameHeader);
                    }
                }
            }

            FileStream OpenFile()
            {
                // TODO handle IOException here on currently active file...
                return System.IO.File.OpenRead(arisFilePath);
            }

            void ValidateFileHeader()
            {
                if (!IsValidFileHeader(arisFilePath, out string reason))
                {
                    throw new Exception($"Invalid file header: '{reason}'; '{arisFilePath}'");
                }
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

            (bool success, ArisFrameHeader frameHeader)
                ReadFrameHeader(FileStream file)
            {
                if (Read(file, out ArisFrameHeader frameHeader))
                {
                    ValidateFrameHeader(frameHeader);
                    return (true, frameHeader);
                }

                return (false, new ArisFrameHeader());
            }
        }

        private static bool Read<T>(Stream stream, out T t)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var readBuffer = new byte[size];

            if (stream.Read(readBuffer) != size)
            {
                t = default;
                return false;
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
