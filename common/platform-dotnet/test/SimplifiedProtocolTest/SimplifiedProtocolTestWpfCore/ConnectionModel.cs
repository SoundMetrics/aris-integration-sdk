using SimplifiedProtocolTest.Helpers;
using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimplifiedProtocolTestWpfCore
{
    public class ConnectionModel : Observable
    {
        public ConnectionModel(string hostname)
        {
            const int commandPort = 56888;
            commandStream = new TcpClient(hostname, commandPort);
            Feedback = "Connected.\n";

            frameReceiver = new UdpClient();
            frameReceiver.Client.ReceiveBufferSize = 2 * 1024 * 1024;
            frameReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            Task.Run(() => ReceiveFramePackets());

            var synchronizationContext = SynchronizationContext.Current;
            Task.Run(() => ReadAndPostFeedback(synchronizationContext));

            InitializeConnection();
        }

        public bool IsConnected { get { return true; } }

        private string feedback = "";

        public string Feedback
        {
            get { return feedback; }
            private set { Set(ref feedback, value); }
        }

        public IObservable<Frame> Frames {  get { return frameAccumulator.Frames; } }

        private /*async*/ void ReceiveFramePackets()
        {
            try
            {
                while (true)
                {
                    //var udpResult = await frameReceiver.ReceiveAsync();
                    //frameAccumulator.ReceivePacket(udpResult.Buffer);

                    // False initialization for call to Receive(ref)
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

                    var bytes = frameReceiver.Receive(ref ep);
                    frameAccumulator.ReceivePacket(bytes);
                }
            }
            catch (ObjectDisposedException)
            {
                // Socket was closed.
            }
        }

        private async void ReadAndPostFeedback(SynchronizationContext synchronizationContext)
        {
            while (true)
            {
                var bytesRead =
                    await commandStream.Client.ReceiveAsync(receiveBuffer, SocketFlags.None);
                if (bytesRead > 0)
                {
                    var s = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                    offUIFeedbackAccumulator = offUIFeedbackAccumulator + s;
                    synchronizationContext.Post(_ => Feedback = offUIFeedbackAccumulator, null);
                }
            }
        }

        private void InitializeConnection()
        {
            var frameReceiverEp = (IPEndPoint)frameReceiver.Client.LocalEndPoint;
            var frameReceiverPort = frameReceiverEp.Port;

            SendCommand(
                commandStream.Client,
                "initialize",
                "salinity brackish",
                "datetime " + ArisDatetime.GetTimestamp(),
                $"rcvr_port {frameReceiverPort}"
                );
        }

        public void StartTestPattern()
        {
            SendCommand(
                commandStream.Client,
                "testpattern");
        }

        public void StartPassiveMode()
        {
            SendCommand(
                commandStream.Client,
                "passive");
        }

        public void StartDefaultAcquireMode()
        {
            SendCommand(
                commandStream.Client,
                "acquire",
                "start_range 1",
                "end_range 5");
        }

        private static void SendCommand(Socket socket, params string[] commandLines)
        {
            var command = string.Join("\n", commandLines) + "\n\n";
            var commandBytes = Encoding.ASCII.GetBytes(command);
            socket.Send(commandBytes);
        }

        private readonly FrameAccumulator frameAccumulator = new FrameAccumulator();

        private byte[] receiveBuffer = new byte[4096];
        private string offUIFeedbackAccumulator = "";
        private TcpClient commandStream;
        private UdpClient frameReceiver;
    }
}
