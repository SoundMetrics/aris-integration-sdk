// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

using SoundMetrics.Aris2.Protocols;
using SoundMetrics.Aris2.Protocols.Commands;
using System.Net;
using System.Net.Sockets;

namespace TestAris2Commands
{
    /// <summary>
    /// Implements a command connection and frame stream with the ARIS.
    /// </summary>
    public sealed class ArisConnection
    {
        private ArisConnection(TcpClient cmdSocket)
        {
            _cmdSocket = cmdSocket;
        }

        public void SendCommand(Command cmd)
        {
            ArisCommands.SendCommand(_cmdSocket, cmd);
        }

        public static bool TryCreate(IPAddress arisIPAddr, out ArisConnection connection)
        {
            connection = null;

            var socket = new TcpClient();
            socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            var ep = new IPEndPoint(arisIPAddr, 56888);
            socket.Connect(ep);

            connection = new ArisConnection(socket);
            return true;
        }

        private readonly TcpClient _cmdSocket;
    }
}
