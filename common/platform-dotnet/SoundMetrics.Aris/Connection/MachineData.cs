using System;
using System.Net;
using System.Net.Sockets;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class MachineData : IDisposable
    {
        public MachineData(IPAddress deviceAddress)
        {
            DeviceAddress = deviceAddress;
        }

        public IPAddress DeviceAddress { get; }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    tcp.Dispose();
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
        private TcpClient tcp;
    }
}
