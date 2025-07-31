using Serilog;
using SoundMetrics.Aris.Connection.Commands;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Core.Raw;
using SoundMetrics.Aris.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class CommandConnection : IDisposable
    {
        public static CommandConnection Create(
            IPAddress deviceAddress,
            SystemType systemType,
            IPEndPoint receiverEndPoint,
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
                //tcp.Client.SetSocketOption(
                //    SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                ControlTcpKeepAlive(tcp.Client);
                tcp.Connect(
                    deviceAddress, NetworkConstants.ArisSonarTcpNOListenPort);

                var settingsCookieFactory = new CookieFactory();
                var io = new ConnectionIO(tcp);
                try
                {
                    tcp = null;

                    Initialize(
                        io,
                        DateTimeOffset.Now,
                        systemType,
                        receiverEndPoint,
                        salinity,
                        settingsCookieFactory);
                    return new CommandConnection(io, settingsCookieFactory);
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

        private CommandConnection(ConnectionIO io, CookieFactory settingsCookieFactory)
        {
            ArgumentNullException.ThrowIfNull(io);
            this.io = io;
            this.settingsCookieFactory = settingsCookieFactory;
        }

        private static void Initialize(
            ConnectionIO io,
            DateTimeOffset currentTime,
            SystemType systemType,
            IPEndPoint receiverEndPoint,
            Salinity salinity,
            CookieFactory settingsCookieFactory)
        {
            Log.Debug("Initializing: receiverEndPoint=[{receiverEndPoint}]", receiverEndPoint);

            var initializeCommand =
                new InitializeDeviceConnection(
                    currentTime,
                    receiverEndPoint,
                    salinity,
                    MakeInitialSettings(systemType, salinity, settingsCookieFactory));
            io.SendCommand(initializeCommand);
        }

        private static ApplySettingsRequest MakeInitialSettings(
            SystemType systemType,
            Salinity salinity,
            CookieFactory settingsCookieFactory)
        {
            var configuration = SystemConfiguration.GetConfiguration(systemType);

            // ### TODO override default observed conditions after first frame
            AcousticSettingsRaw rawSettings = configuration.GetDefaultSettings(ObservedConditions.Default, salinity);
            var cookie = settingsCookieFactory.GetNextCookie();
            return new ApplySettingsRequest(SettingsCookie: cookie, Settings: rawSettings);
        }

        public void RequestAcousticSettings(AcousticSettingsRaw acousticSettingsRaw)
        {
            io.SendCommand(
                new ApplySettingsRequest(GetNextSettingsCookie(), acousticSettingsRaw));
        }

        private uint GetNextSettingsCookie()
        {
            return (uint)Interlocked.Increment(ref nextSettingsCookie);
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
        private readonly CookieFactory settingsCookieFactory;

        private bool disposed;
        private int nextSettingsCookie = 2;
    }
}
