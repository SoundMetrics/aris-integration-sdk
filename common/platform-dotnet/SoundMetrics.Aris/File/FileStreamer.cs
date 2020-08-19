using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Device;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;

namespace SoundMetrics.Aris.File
{
    /// <summary>
    /// Observes a stream of files and conditionally writes desired frames
    /// to a file.
    /// </summary>
    public sealed class FileStreamer : IDisposable
    {
        public FileStreamer(
            IObservable<Frame> frames,
            string filePath,
            int earliestAllowedCookie,
            SynchronizationContext syncContext)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            this.filePath = filePath;

            frameSub = frames
                .Where(frame => frame.FrameHeader.AppliedSettings >= earliestAllowedCookie)
                .ObserveOn(syncContext)
                .Subscribe(OnCurrentFrame);
        }

        private void OnCurrentFrame(Frame frame)
        {
            if (allowedGeometry is SampleGeometry expectedGeometry)
            {
                if (SonarConfig.GetSampleGeometry(frame.FrameHeader) == expectedGeometry)
                {
                    Debug.Assert(!(fileWriter is null));
                    fileWriter.WriteFrame(frame);
                }
            }
            else
            {
                Debug.Assert(fileWriter is null);
                allowedGeometry = SonarConfig.GetSampleGeometry(frame.FrameHeader);
                fileWriter = FileWriter.CreateNewWithFrame(frame, filePath);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
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

        private readonly string filePath;
        private readonly IDisposable frameSub;

        private bool disposed;
        private FileWriter? fileWriter;
        private SampleGeometry? allowedGeometry = default;
    }
}
