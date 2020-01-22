using SimplifiedProtocolTest.Helpers;
using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimplifiedProtocolTest
{
    public class ConnectionModel : Observable
    {
        public ConnectionModel(string hostname)
        {
            const int commandPort = 56888;
            commandStream = new TcpClient(hostname, commandPort);
            Feedback = "Connected.\n";

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

        public IObservable<object> Frames {  get { return frameSubject; } }

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
            SendCommand(
                commandStream.Client,
                "initialize",
                "salinity brackish",
                "datetime " + ArisDatetime.GetTimestamp(),
                "rcvr_port " + "56999"
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

        private byte[] receiveBuffer = new byte[4096];
        private string offUIFeedbackAccumulator = "";
        private TcpClient commandStream;
        private UdpClient frameReceiver;
        private Subject<object> frameSubject = new Subject<object>();
    }
}
