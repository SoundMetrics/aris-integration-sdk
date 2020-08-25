using SoundMetrics.Aris.Device;
using SoundMetrics.Aris.Network;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var tcp = new TcpClient(AddressFamily.InterNetwork);

            try
            {
                tcp.Client.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                tcp.Connect(
                    deviceAddress, NetworkConstants.ArisSonarTcpNOListenPort);

                ControlTcpKeepAlives(
                    tcp.Client,
                    interval: TimeSpan.FromSeconds(5),
                    retryInterval: TimeSpan.FromSeconds(1));

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

            // Using this, not Socket.IOControl, for cross-platform use.
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
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
