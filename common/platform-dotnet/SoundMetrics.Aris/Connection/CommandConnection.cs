using Serilog;
using SoundMetrics.Aris.Network;
using System;
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

                InitializeSimplifiedProtocol(tcp);
                return new CommandConnection(tcp);
            }
            catch
            {
                tcp.Dispose();
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

        private CommandConnection(TcpClient tcp)
        {
            if (tcp is null) throw new ArgumentNullException(nameof(tcp));

            this.tcp = tcp;
            // Wire things up
            throw new NotImplementedException();
        }

        private static void InitializeSimplifiedProtocol(TcpClient tcp)
        {
            if (SendCommand(tcp, "initialize", out var response, out var error))
            {
                Log.Debug("Initialize response=[{response}]", response);
            }
            else
            {
                throw new Exception("Protocl initialization failed: " + error);
            }
        }

        private static bool SendCommand(
            TcpClient tcp,
            string command,
            out string response,
            out string error)
        {
            var octets = ASCIIEncoding.ASCII.GetBytes(command + "\n\n");
            if (tcp.Client.Send(octets) == octets.Length)
            {
                var buffer = new byte[1024];
                if (tcp.Client.Receive(buffer) > 0)
                {
                    response = ASCIIEncoding.ASCII.GetString(buffer);
                    var splits = response.Split(new char[] { ' ', '\t', '\r', '\n' });
                    if (splits[0] == "200")
                    {
                        error = "";
                        return true;
                    }
                    else
                    {
                        error = response;
                        response = "";
                        return false;
                    }
                }
            }

            response = "";
            error = "Network transaction failed";
            return false;
        }

        public bool SendCommand(string command, out string response)
        {
            return SendCommand(command, out response);
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
