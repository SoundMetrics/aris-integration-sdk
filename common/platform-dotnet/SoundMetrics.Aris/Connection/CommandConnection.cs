using Serilog;
using SoundMetrics.Aris.Network;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class CommandConnection : IDisposable
    {
        public static bool Create(IPAddress ipAddress, out CommandConnection connection)
        {
            var tcp = new TcpClient();
            try
            {
                tcp.Client.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                tcp.Connect(
                    ipAddress, NetworkConstants.ArisSonarTcpNOListenPort);

                ControlTcpKeepAlives(
                    tcp.Client,
                    interval: TimeSpan.FromSeconds(5),
                    retryInterval: TimeSpan.FromSeconds(1));

                connection = new CommandConnection(tcp);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Couldn't connect to {ipAddress}: {exMessage}",
                    ipAddress, ex.Message);

                connection = null;
                return false;
            }
        }

        private static void ControlTcpKeepAlives(
            Socket client,
            TimeSpan interval,
            TimeSpan retryInterval)
        {
            // Rather than creating a packed struct here we'll just construct the byte array directly.
            var enable = 1u;
            var intervalMillis = (uint)interval.TotalMilliseconds;
            var retryIntervalMillis = (uint)retryInterval.TotalMilliseconds;

            var payload =
                BitConverter.GetBytes(enable)
                    .Concat(BitConverter.GetBytes(intervalMillis))
                    .Concat(BitConverter.GetBytes(retryIntervalMillis))
                    .ToArray();
            _ = client.IOControl(IOControlCode.KeepAliveValues, payload, null);
        }

        private CommandConnection(TcpClient tcp)
        {
            if (tcp is null) throw new ArgumentNullException(nameof(tcp));

            this.tcp = tcp;
            // Wire things up
            throw new NotImplementedException();
        }

        public (bool success, string response) SendCommand(string command)
        {
            return default;
        }

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

        private readonly TcpClient tcp;

        private bool disposed;
    }
}
