using Serilog;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.File
{
    using static Serialization;

    /// <summary>
    /// Write a stream of frames to a file.
    /// </summary>
    public sealed class FileWriter : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="FileWriter"/> and associated empty file.
        /// All frames in the file must have the same <see cref="SampleGeometry"/>
        /// as the first frame.
        /// </summary>
        /// <param name="frameHeader">The frame header used to derive the file header.</param>
        /// <param name="filePath">The path of the file to be created.</param>
        /// <returns></returns>
        public static FileWriter CreateEmpty(in FrameHeader frameHeader, string filePath) =>
            CreateNew(frameHeader, filePath);

        /// <summary>
        /// Creates a new <see cref="FileWriter"/> and writes its first frame to
        /// a new file.
        /// All frames in the file must have the same <see cref="SampleGeometry"/>
        /// as the first frame.
        /// </summary>
        /// <param name="firstFrame">The first frame to be written.</param>
        /// <param name="filePath">The path of the file to be created.</param>
        /// <returns></returns>
        public static FileWriter CreateNewWithFrame(Frame firstFrame, string filePath)
        {
            var writer = CreateNew(firstFrame.FrameHeader, filePath);

            try
            {
                writer.WriteFrame(firstFrame);
                return writer;
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't write the first frame: '{message}'", ex.Message);
                writer.Dispose();
                throw;
            }
        }

        internal static FileWriter CreateNew(in FrameHeader firstFrameHeader, string filePath)
        {
            var stream = new FileStream(
                filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            return new FileWriter(firstFrameHeader, stream);
        }

        [Obsolete("Not yet implemented")]
        internal static FileWriter Append(in SampleGeometry sampleGeometry, string filePath)
        {
            // Will need to position correctly after the last complete frame,
            // not assuming the file is not somehow truncated.
            throw new NotImplementedException();
        }

        private FileWriter(
            in FrameHeader firstFrameHeader,
            FileStream fileStream)
        {
            try
            {
                if (SystemConfiguration.TryGetSampleGeometry(firstFrameHeader, out var sampleGeometry))
                {
                    this.sampleGeometry = sampleGeometry;
                    this.fileStream = fileStream;

                    WriteNewFileHeader(fileStream, firstFrameHeader, sampleGeometry);
                }
                else
                {
                    throw new Exception("Couldn't determine sample geometry");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't initialize the file: '{message}'", ex.Message);
                fileStream.Close();
                throw;
            }
        }

        /// <summary>
        /// Writes a frame to the file. The <paramref name="frame"/>'s
        /// <see cref="SampleGeometry"/> must match that
        /// of the first frame written to the file. All frames within the file
        /// must have the same geometry.
        /// </summary>
        /// <param name="frame">The frame to be written.</param>
        /// <returns>The assigned frame index for the written frame.</returns>
        public int WriteFrame(Frame frame)
        {
            var startPosition = fileStream.Position;
            var success = false;

            try
            {
                if (SystemConfiguration.TryGetSampleGeometry(frame.FrameHeader, out var geometry))
                {
                    if (geometry != sampleGeometry)
                    {
                        throw new InvalidOperationException(
                            "Cannot change the sample geometry within a recording");
                    }

                    var frameIndex = frameCount;
                    var headerForUpdate = frame.FrameHeader;
                    headerForUpdate.FrameIndex = frameIndex;

                    fileStream.WriteStruct(headerForUpdate);
                    fileStream.Write(frame.Samples.Span);

                    ++frameCount;
                    success = true;

                    return (int)frameIndex;
                }
                else
                {
                    throw new Exception("Couldn't determine sample geometry");
                }
            }
            finally
            {
                if (!success)
                {
                    fileStream.Position = startPosition;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    UpdateFileHeader(fileStream, frameCount);
                    CleanUpFile(fileStream);
                    fileStream.Close();
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

        private static void WriteNewFileHeader(
            FileStream stream,
            in FrameHeader firstFrameHeader,
            SampleGeometry geometry)
        {
            var fileHeader = new FileHeader
            {
                SN = firstFrameHeader.SonarSerialNumber,
                Version = FileHeader.ArisFileSignature,
                FrameCount = 0,
                NumRawBeams = (uint)geometry.BeamCount,
                SamplesPerChannel = (uint)geometry.SamplesPerBeam,
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

        private static void CleanUpFile(FileStream fileStream)
        {
            try
            {
                // If the file has no frames, leave it empty, but leave the empty
                // file in place as a tombstone.
                if (fileStream.Length <= Marshal.SizeOf<FrameHeader>())
                {
                    fileStream.SetLength(0);
                }
            }
            catch (Exception ex)
            {
                // Don't propagate additional issues on shutdown, just log it.
                Log.Error("Could not clean up file: '{message}'", ex.Message);
            }
        }

        private readonly FileStream fileStream;

        /// <summary>
        /// The one and only frame geometry allowed throughout the file.
        /// </summary>
        private readonly SampleGeometry sampleGeometry;

        private uint frameCount;
        private bool disposed;
    }
}
