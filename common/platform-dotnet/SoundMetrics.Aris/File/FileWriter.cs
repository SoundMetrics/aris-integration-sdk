using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Device;
using System;
using System.IO;

namespace SoundMetrics.Aris.File
{
    using static Serialization;

    public sealed class FileWriter : IDisposable
    {
        public FileWriter CreateNew(in SampleGeometry sampleGeometry, string filePath)
        {
            var stream = new FileStream(
                filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            return new FileWriter(sampleGeometry, stream);
        }

        [Obsolete("Not yet implemented")]
        public FileWriter Append(in SampleGeometry sampleGeometry, string filePath)
        {
            // Will need to position correctly after the last complete frame,
            // not assuming the file is not somehow truncated.
            throw new NotImplementedException();
        }

        private FileWriter(in SampleGeometry sampleGeometry, FileStream stream)
        {
            this.sampleGeometry = sampleGeometry;
            this.outputStream = stream;

            WriteNewFileHeader(stream);
        }

        public void WriteFrame(Frame frame)
        {
            var startPosition = outputStream.Position;
            var success = false;

            try
            {
                var (beamCount, samplesPerBeam, _, _) =
                    SonarConfig.GetSampleGeometry(frame.FrameHeader);

                if (beamCount != sampleGeometry.BeamCount || samplesPerBeam != sampleGeometry.SamplesPerBeam)
                {
                    throw new InvalidOperationException(
                        "Cannot change the sample geometry within a recording");
                }

                outputStream.WriteStruct(frame.FrameHeader);
                outputStream.Write(frame.Samples.Span);

                ++frameCount;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    outputStream.Position = startPosition;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    UpdateFileHeader(outputStream, frameCount);
                    outputStream.Close();
                }

                // No unmanaged resources to clean up.

                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static void WriteNewFileHeader(FileStream stream)
        {
            var fileHeader = new FileHeader
            {
                Version = FileHeader.ArisFileSignature,
                FrameCount = 0,
            };

            stream.WriteStruct(fileHeader);
        }

        private static bool UpdateFileHeader(FileStream stream, uint frameCount)
        {
            var startingPosition = stream.Position;

            try
            {
                stream.Position = 0;
                if (stream.ReadStruct(out FileHeader fileHeader))
                {
                    fileHeader.FrameCount = frameCount;

                    stream.Position = 0;
                    stream.WriteStruct(fileHeader);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                stream.Position = startingPosition;
            }
        }

        private readonly FileStream outputStream;
        private readonly SampleGeometry sampleGeometry;
        private uint frameCount;
        private bool disposed;
    }
}
