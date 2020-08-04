using System;
using System.Net.Sockets;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class ArisConnection : IDisposable
    {
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    tcp?.Dispose();
                }

                // no unmanaged resources
                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool disposed;
        private ConnectionState state = ConnectionState.Start;
        private TcpClient tcp;
    }
}
