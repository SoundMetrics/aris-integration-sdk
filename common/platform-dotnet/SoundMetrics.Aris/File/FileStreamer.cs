﻿using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Device;
using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace SoundMetrics.Aris.File
{
    /// <summary>
    /// Observes a stream of frames and conditionally writes desired frames
    /// to a file.
    /// </summary>
    public sealed class FileStreamer : IDisposable
    {
        public FileStreamer(
            IObservable<Frame> frames,
            string outputPath,
            int earliestAllowedCookie)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            this.outputPath = outputPath;

            incomingQueue = new BufferedMessageQueue<Frame>(HandleIncomingFrame);

            frameSub = frames
                .Where(frame =>
                    Math.Max(frame.FrameHeader.AppliedSettings, frame.FrameHeader.ConstrainedSettings)
                        >= earliestAllowedCookie)
                .Subscribe(frame => incomingQueue.Post(frame));
        }

        public string OutputPath => outputPath;

        private void HandleIncomingFrame(Frame frame)
        {
            if (allowedGeometry is SampleGeometry expectedGeometry)
            {
                Debug.Assert(!(fileWriter is null));

                if (SonarConfig.GetSampleGeometry(frame.FrameHeader) == expectedGeometry)
                {
                    fileWriter.WriteFrame(frame);
                }
            }
            else
            {
                Debug.Assert(fileWriter is null);
                allowedGeometry = SonarConfig.GetSampleGeometry(frame.FrameHeader);
                fileWriter = FileWriter.CreateNewWithFrame(frame, outputPath);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    incomingQueue.Dispose();
                    frameSub?.Dispose();
                    fileWriter?.Dispose();
                }

                // no free unmanaged resources (unmanaged objects)
                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private readonly string outputPath;
        private readonly IDisposable frameSub;
        private readonly BufferedMessageQueue<Frame> incomingQueue;

        private bool disposed;
        private FileWriter? fileWriter;
        private SampleGeometry? allowedGeometry = default;
    }
}
