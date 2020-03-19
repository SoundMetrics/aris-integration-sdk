using SimplifiedProtocolTest.Helpers;
using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimplifiedProtocolTestWpfCore
{
    public sealed class ConnectionModel : Observable, ITestOperations
    {
        public ConnectionModel(string hostname)
        {
            feedbackSubject = new Subject<string>();
            feedbackReceiverSub = feedbackSubject.Subscribe(feedback =>
                {

                    PostExplanatoryText(MakeIndentedFeedback(feedback));

                    string MakeIndentedFeedback(string lines)
                    {
                        return
                            String.Join("\n",
                                lines.Split("\n").Select(line => "| " + line)
                            )
                            + "\n";
                    }
                });

            const int commandPort = 56888;
            commandStream = new TcpClient(hostname, commandPort);
            Feedback = "Connected.\n";

            frameReceiver = new UdpClient();
            frameReceiver.Client.ReceiveBufferSize = 2 * 1024 * 1024;
            frameReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            Task.Run(() => ReceiveFramePackets());

            synchronizationContext = SynchronizationContext.Current;
            Task.Run(() => ReadAndPostFeedback());

            InitializeConnection();
        }

        public bool IsConnected { get { return true; } }

        private string feedback = "";

        public string Feedback
        {
            get { return feedback; }
            private set { Set(ref feedback, value); }
        }

        public IObservable<Frame> Frames { get { return frameAccumulator.Frames; } }

        private /*async*/ void ReceiveFramePackets()
        {
            try
            {
                while (true)
                {
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

        private async void ReadAndPostFeedback()
        {
            while (true)
            {
                var bytesRead =
                    await commandStream.Client.ReceiveAsync(feedbackReceiveBuffer, SocketFlags.None);
                if (bytesRead > 0)
                {
                    var s = Encoding.ASCII.GetString(feedbackReceiveBuffer, 0, bytesRead);
                    feedbackSubject.OnNext(s);
                }
            }
        }

        private void PostExplanatoryText(string message)
        {
            SendOrPostCallback performUpdate = _ =>
            {
                // All updates are queued to the UI thread.
                offUIFeedbackAccumulator = offUIFeedbackAccumulator + message;
                Feedback = offUIFeedbackAccumulator;
            };

            synchronizationContext?.Post(performUpdate, null);
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

        private static string FormatCommand(string[] commandLines) =>
            string.Join("\n", commandLines) + "\n\n";

        private static string FormatCommandForLogging(string[] commandLines) =>
            string.Join("\\n\n", commandLines) + "\\n\n\\n\n";

        private void SendCommand(
            Socket socket,
            params string[] commandLines)
        {
            PostCommandText(commandLines);

            var command = FormatCommand(commandLines);
            var commandBytes = Encoding.ASCII.GetBytes(command);
            socket.Send(commandBytes);

            void PostCommandText(string[] commandLines)
            {
                var commandText =
                    "\n"
                    + "Sending command:\n"
                    + ">>>>>>>>\n"
                    + FormatCommandForLogging(commandLines)
                    + "<<<<<<<<\n";
                PostExplanatoryText(commandText);
            }
        }

        private readonly FrameAccumulator frameAccumulator = new FrameAccumulator();
        private readonly SynchronizationContext? synchronizationContext;
        private readonly Subject<string> feedbackSubject;
        private readonly IDisposable feedbackReceiverSub;

        private byte[] feedbackReceiveBuffer = new byte[4096];
        private string offUIFeedbackAccumulator = "";
        private TcpClient commandStream;
        private UdpClient frameReceiver;
    }
}
