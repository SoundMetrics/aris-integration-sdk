using SoundMetrics.Aris.Device;
using SoundMetrics.Aris.Network;
using System;
using System.Net;
using System.Net.Sockets;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class CommandConnection : IDisposable
    {
        public static CommandConnection Create(
            IPAddress deviceAddress,
            int receiverPort,
            Salinity salinity)
        {
            // ARIS is currently IPv4 only. We don't need to specify this when
            // constructing the TcpClient, but it keeps the local address from
            // confusing the natives in-house--it will appear in logs as IPv4
            // (169.254.31.178:51136) rather than IPv6
            // ([::ffff:169.254.31.178%16]:51136).
            TcpClient? tcp = new TcpClient(AddressFamily.InterNetwork);

            try
            {
                tcp.Client.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                tcp.Connect(
                    deviceAddress, NetworkConstants.ArisSonarTcpNOListenPort);

                ControlTcpKeepAlive(tcp.Client);

                var io = new ConnectionIO(tcp);
                try
                {
                    tcp = null;

                    InitializeSimplifiedProtocol(
                        io, DateTimeOffset.Now, receiverPort, salinity);
                    return new CommandConnection(io);
                }
                catch
                {
                    io.Dispose();
                    throw;
                }
            }
            catch
            {
                tcp?.Dispose();
                throw;
            }
        }

        public IPEndPoint LocalEndpoint => io.LocalEndpoint;

        // This is perhaps gold-plating, as the ARIS can have zombie connections.
        // In other words, it will send keep-alives after the process is gone.
        private static void ControlTcpKeepAlive(Socket client)
        {
            // Using SetSocketOption, not Socket.IOControl, for cross-platform use.
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 2);
            client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 2);
            client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 1);
        }

        private CommandConnection(ConnectionIO io)
        {
            if (io is null) throw new ArgumentNullException(nameof(io));

            this.io = io;
        }

        private static void InitializeSimplifiedProtocol(
            ConnectionIO io,
            DateTimeOffset currentTime,
            int receiverPort,
            Salinity salinity)
        {
            var initializeCommand =
                new InitializeCommand(currentTime, receiverPort, salinity);
            var response = io.SendCommand(initializeCommand);

            if (!response.IsSuccessful)
            {
                var joinedResponseText = string.Join("\n", response.ResponseText);
                throw new Exception("Protocol initialization failed: " + joinedResponseText);
            }
        }

        public CommandResponse SendCommand(ICommand command)
        {
            return io.SendCommand(command);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    io.Dispose();
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

        private readonly ConnectionIO io;

        private bool disposed;
    }
}
