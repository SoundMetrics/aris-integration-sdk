using Serilog;
using SoundMetrics.Aris.Connection.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class ConnectionIO : IDisposable
    {
        /// <summary>
        /// Constructs the IO bits and takes ownership of the TcpClient.
        /// </summary>
        public ConnectionIO(TcpClient tcp)
        {
            this.tcp = tcp ?? throw new ArgumentNullException(nameof(tcp));
            this.LocalEndpoint = GetSafeLocalEndPoint(tcp);

            try
            {
                stream = new NetworkStream(tcp.Client);
            }
            catch
            {
                stream?.Dispose();
                tcp.Dispose();
                throw;
            }
        }

        private static IPEndPoint GetSafeLocalEndPoint(TcpClient tcp)
        {
            if (tcp is null)
            {
                throw new ArgumentNullException(nameof(tcp));
            }

            Debug.Assert(tcp.Client is not null);

            var ep = tcp.Client.LocalEndPoint;
            return ep is null ? new IPEndPoint(IPAddress.Loopback, 42) : (IPEndPoint)ep;
        }

        public IPEndPoint LocalEndpoint { get; }

        public void SendCommand(IOutgoingCommand command)
        {
            var messageParts = OutgoingCommandGenerator.GenerateOutgoingCommands(command);
            Debug.Assert(messageParts.Count > 0);

            foreach (var messagePart in messageParts)
            {
                stream.Write(messagePart);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    stream.Dispose();
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

        private readonly TcpClient tcp;
        private readonly NetworkStream stream;

        private bool disposed;
    }
}
