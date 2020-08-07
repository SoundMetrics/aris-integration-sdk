using Serilog;
using SoundMetrics.Aris.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class CommandConnection : IDisposable
    {
        public static CommandConnection Create(IPAddress ipAddress)
        {
            var tcp = new TcpClient();
            ConnectionIO io = null;

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

                io = new ConnectionIO(tcp);
                tcp = null;

                InitializeSimplifiedProtocol(io);
                return new CommandConnection(io);
            }
            catch
            {
                tcp?.Dispose();
                io?.Dispose();
                throw;
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

        private CommandConnection(ConnectionIO io)
        {
            if (io is null) throw new ArgumentNullException(nameof(io));

            this.io = io;
        }

        private static string[] initializeCommand = new[] { "initialize" };

        private static void InitializeSimplifiedProtocol(ConnectionIO io)
        {
            var response = io.SendCommand(initializeCommand);
            var joinedResponseText = string.Join("\n", response.ResponseText);

            if (response.IsSuccessful)
            {
                Log.Debug("Initialize response=[{response}]", joinedResponseText);
            }
            else
            {
                throw new Exception("Protocol initialization failed: " + joinedResponseText);
            }
        }

        public CommandResponse SendCommand(IEnumerable<string> command)
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
