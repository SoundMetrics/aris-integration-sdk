using System;
using System.Net;
using System.Net.Sockets;
using Command = SoundMetrics.Aris2.Protocols.Commands.Command;

namespace SoundMetrics.Aris2.Protocols
{
    /// <summary>
    /// Provides static methods for the correct construction of ARIS 2 commands.
    /// Contains functions to create the most commonly used commands that may be sent to
    /// and ARIS 2.
    /// </summary>
    public static class ArisCommands
    {
        /// <summary>
        /// Converts a command to a byte array and sends it to the sonar.
        /// </summary>
        /// <param name="commandSocket">A TCP socket connected to the sonar
        /// on the command port.</param>
        /// <param name="cmd">The <c>Command</c> to be sent.</param>
        public static void SendCommand(TcpClient commandSocket, Command cmd)
        {
            var prefixedMessage = ToPrefixedBytes(cmd);
            commandSocket.Client.Send(prefixedMessage); 
        }

        /// <summary>
        /// Creates a buffer of bytes representing a command. This is sent to the ARIS
        /// to command it. This buffer is prefixed with the length prefix, so it can be
        /// sent directly to the ARIS, or sent via <c>SendCommand</c>.
        /// </summary>
        /// <param name="cmd">The command to be sent.</param>
        /// <returns>The command as bytes.</returns>
        private static byte[] ToPrefixedBytes(Command cmd)
        {
            byte[] msg = cmd.ToByteArray();
            byte[] prefix = GetCommandLengthPrefix(msg);
            byte[] buf = new byte[msg.Length + prefix.Length];

            Array.Copy(prefix, buf, prefix.Length);
            Array.Copy(msg, 0, buf, prefix.Length, msg.Length);

            return buf;

            byte[] GetCommandLengthPrefix(byte[] serializedCommand)
            {
                // htonl() if you're writing C:
                var msgLengthNetworkOrder = IPAddress.HostToNetworkOrder(serializedCommand.Length);
                return BitConverter.GetBytes(msgLengthNetworkOrder);
            }
        }

        /// <summary>
        /// Builds a command to set the clock on the sonar. The clock is set to local time.
        /// </summary>
        /// <param name="dateTime">The current time.</param>
        /// <returns>A <c>SET_DATETIME</c> command.</returns>
        public static Command BuildTimeCmd(DateTime dateTime)
        {
            Command.Types.SetDateTime BuildDateTime()
            {
                var inner = Command.Types.SetDateTime.CreateBuilder();
                inner.DateTime = dateTime.ToLocalTime().ToString("yyyy'-'MMM'-'dd HH':'mm':'ss",
                                    System.Globalization.CultureInfo.InvariantCulture);
                return inner.Build();
            }

            var outer = Command.CreateBuilder();
            outer.Type = Command.Types.CommandType.SET_DATETIME;
            outer.DateTime = BuildDateTime();
            return outer.Build();
        }

        /// <summary>
        /// Builds a command to set the frame stream receiver. When using this function,
        /// the IP address to which the framestream is sent is implicitly assumed to be
        /// the IP address of your controller application.
        /// </summary>
        /// <param name="port">The port to which the frame stream is sent.</param>
        /// <returns>A <c>SET_FRAMESTREAM_RECEIVER</c> command.</returns>
        public static Command BuildSetFrameStreamReceiverCmd(uint port)
        {
            var builder = Command.CreateBuilder();
            builder.Type = Command.Types.CommandType.SET_FRAMESTREAM_RECEIVER;
            builder.FrameStreamReceiver = BuildFramestreamReceiver(port);
            return builder.Build();
        }

        /// <summary>
        /// Builds a command to set the frame stream receiver. <paramref name="ipAddress"/> must
        /// be an IPv4 address in the form <c>"a.b.c.d"</c>.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static Command BuildSetFrameStreamReceiverCmd(uint port, string ipAddress)
        {
            var builder = Command.CreateBuilder();
            builder.Type = Command.Types.CommandType.SET_FRAMESTREAM_RECEIVER;
            builder.FrameStreamReceiver = BuildFramestreamReceiver(port, ipAddress);
            return builder.Build();
        }

        private static Command.Types.SetFrameStreamReceiver BuildFramestreamReceiver(uint port, string ipAddress = "")
        {
            var hasIpAddress = !string.IsNullOrWhiteSpace(ipAddress);
            if (hasIpAddress)
            {
                if (IPAddress.TryParse(ipAddress, out var addr))
                {
                    if (addr.GetAddressBytes().Length != 4)
                    {
                        throw new ArgumentException("IP addressmust be an IPv4 address", "ipAddress");
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid value, does not parse.", "ipAddress");
                }
            }

            var builder = Command.Types.SetFrameStreamReceiver.CreateBuilder();
            builder.Port = port;

            if (hasIpAddress)
            {
                builder.Ip = ipAddress;
            }

            return builder.Build();
        }

        /// <summary>
        /// See the main SDK documentation for a discussion of acoustic settings.
        /// </summary>
        public struct AcousticSettingsWithCookie
        {
            public uint Cookie;
            public float FrameRate;
            public uint SampleCount;
            public uint SampleStartDelay;
            public uint CyclePeriod;
            public uint SamplePeriod;
            public uint PulseWidth;
            public uint PingMode;
            public bool EnableTransmit;
            public bool HighFrequency;
            public bool Enable150Volts;
            public float ReceiverGain;
        }

        private const Command.Types.SetAcousticSettings.Types.Frequency
            LowFrequency = Command.Types.SetAcousticSettings.Types.Frequency.LOW,
            HighFrequency = Command.Types.SetAcousticSettings.Types.Frequency.HIGH;

        /// <summary>
        /// Builds a command to set acoustic settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>A <c>SET_ACOUSTICS</c> command.</returns>
        public static Command BuildAcousticSettingsCmd(AcousticSettingsWithCookie settings)
        {
            Command.Types.SetAcousticSettings BuildSettings()
            {
                var inner = Command.Types.SetAcousticSettings.CreateBuilder();
                inner.Cookie = settings.Cookie;
                inner.FrameRate = settings.FrameRate;
                inner.SamplesPerBeam = settings.SampleCount;
                inner.SampleStartDelay = settings.SampleStartDelay;
                inner.CyclePeriod = settings.CyclePeriod;
                inner.SamplePeriod = settings.SamplePeriod;
                inner.PulseWidth = settings.PulseWidth;
                inner.PingMode = settings.PingMode;
                inner.EnableTransmit = settings.EnableTransmit;
                inner.Frequency = settings.HighFrequency ? HighFrequency : LowFrequency;
                inner.Enable150Volts = settings.Enable150Volts;
                inner.ReceiverGain = settings.ReceiverGain;
                return inner.Build();
            }

            var outer = Command.CreateBuilder();
            outer.Type = Command.Types.CommandType.SET_ACOUSTICS;
            outer.Settings = BuildSettings();
            return outer.Build();
        }

        /// <summary>
        /// Builds a command to set the focus range.
        /// </summary>
        /// <param name="range">The focus range in meters.</param>
        /// <returns>A <c>SET_FOCUS</c> command.</returns>
        public static Command BuildFocusCmd(float range)
        {
            Command.Types.SetFocusPosition BuildFocusPosition()
            {
                var inner = Command.Types.SetFocusPosition.CreateBuilder();
                inner.FocusRange = range;
                return inner.Build();
            }

            var outer = Command.CreateBuilder();
            outer.Type = Command.Types.CommandType.SET_FOCUS;
            outer.FocusPosition = BuildFocusPosition();
            return outer.Build();
        }

        public enum Salinity { Fresh = 0, Brackish = 15, Seawater = 35 };

        /// <summary>
        /// Build a command to set salinity on the sonar.
        /// </summary>
        /// <param name="salinity">The salinity.</param>
        /// <returns>A <c>SET_SALINITY</c> command.</returns>
        public static Command BuildSalinityCmd(Salinity salinity)
        {
            Command.Types.SetSalinity BuildSalinity()
            {
                var inner = Command.Types.SetSalinity.CreateBuilder();
                inner.Salinity = (Command.Types.SetSalinity.Types.Salinity)salinity;
                return inner.Build();
            }

            var outer = Command.CreateBuilder();
            outer.Type = Command.Types.CommandType.SET_SALINITY;
            outer.Salinity = BuildSalinity();
            return outer.Build();
        }

        /// <summary>
        /// Builds a ping command.
        /// </summary>
        /// <returns>A <c>PING</c> command.</returns>
        public static Command BuildPingCmd()
        {
            return PingCommand.Value;
        }

        private static readonly Lazy<Command> PingCommand = new Lazy<Command>(() =>
        {
            var builder = Command.CreateBuilder();
            builder.Type = Command.Types.CommandType.PING;
            builder.Ping = Command.Types.Ping.CreateBuilder().Build();
            return builder.Build();
        });

        /// <summary>
        /// Builds a command to set the frame stream settings.
        /// Interpacket delay helps some slower network equipment avoid dropping packets.
        /// </summary>
        /// <param name="interpacketDelay">The extra delay between frame parts in microseconds.
        /// This is normally set to zero.</param>
        /// <returns>A <c>SET_FRAMESTREAM_SETTINGS</c> command.</returns>
        public static Command BuildFrameStreamSettingsCmd(uint interpacketDelay)
        {
            Command.Types.SetInterpacketDelay BuildDelay()
            {
                var delay = Command.Types.SetInterpacketDelay.CreateBuilder();
                delay.Enable = interpacketDelay > 0;
                if (interpacketDelay > 0)
                {
                    delay.DelayPeriod = interpacketDelay;
                }

                return delay.Build();
            }

            Command.Types.SetFrameStreamSettings BuildSettings()
            {
                var settings = Command.Types.SetFrameStreamSettings.CreateBuilder();
                settings.InterpacketDelay = BuildDelay();
                return settings.Build();
            }

            var outer = Command.CreateBuilder();
            outer.Type = Command.Types.CommandType.SET_FRAMESTREAM_SETTINGS;
            outer.FrameStreamSettings = BuildSettings();
            return outer.Build();
        }
    }
}
