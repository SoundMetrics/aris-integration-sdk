using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Device;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.File
{
    internal class FileTraits
    {
        public long FileLength;
        public SampleGeometry Geometry;
        public int SerializedFrameSize;
        public int FileHeaderFrameCount;
        public double CalculatedFrameCount;
        public int RemainederBytes;

        public static bool GetFileTraits(string path, out FileTraits? fileTraits)
        {
            using var stream = System.IO.File.OpenRead(path);
            return GetFileTraits(stream, out fileTraits);
        }

        private static bool GetFileTraits(Stream stream, out FileTraits? fileTraits)
        {
            var startingPosition = stream.Position;
            stream.Position = 0;

            try
            {
                var fileSize = stream.Length;

                if (ArisRecording.ReadFileHeader(
                        stream,
                        out var fileHeader,
                        out var reason)
                    && ArisRecording.ReadFrameHeader(
                        stream,
                        out var frameHeader))
                {
                    var fileHeaderSize = Marshal.SizeOf<FileHeader>();
                    var frameHeaderSize = Marshal.SizeOf<FrameHeader>();

                    var geometry = SonarConfig.GetSampleGeometry(frameHeader);
                    var serializedFrameSize = geometry.TotalSampleCount + frameHeaderSize;
                    var calculatedFrameCount =
                        (double)(fileSize - fileHeaderSize) / serializedFrameSize;
                    var remainderBytes = (int)
                        (fileSize
                            - fileHeaderSize
                            - (serializedFrameSize * Math.Floor(calculatedFrameCount)));

                    fileTraits = new FileTraits
                    {
                        FileLength = fileSize,
                        Geometry = geometry,
                        SerializedFrameSize = serializedFrameSize,
                        FileHeaderFrameCount = (int)fileHeader.FrameCount,
                        CalculatedFrameCount = calculatedFrameCount,
                        RemainederBytes = remainderBytes,
                    };
                    return true;
                }
                else
                {
                    fileTraits = null;
                    return false;
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
