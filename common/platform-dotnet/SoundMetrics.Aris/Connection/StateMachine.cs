using System;
using System.Net.Sockets;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class StateMachine : IDisposable
    {
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ShutDown();
                    tcp.Dispose();
                }

                // no unmanaged resources
                disposed = true;
            }
        }

        private void ShutDown()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private TcpClient tcp;
        private bool disposed;
        private ConnectionState state = ConnectionState.Start;
    }
}
