﻿using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimplifiedProtocolTestWpfCore
{
    public sealed class ConnectionModel
        : SimplifiedProtocolTest.Helpers.Observable, ITestOperations
    {
        public ConnectionModel(string hostname)
        {
            feedbackSubject = new Subject<string>();
            feedbackReceiverSub = feedbackSubject.Subscribe(feedback =>
                {

                    PostExplanatoryText(
                        MakeIndentedFeedback(feedback));

                    string MakeIndentedFeedback(string lines)
                    {
                        return
                            String.Join("\n",
                                lines.Split("\n").Select(line => "| " + line)
                            )
                            + "\n";
                    }
                });

            parsedFeedbackSubject = new Subject<ParsedFeedbackFromSonar>();
            parsedFeedbackObserver = feedbackSubject.Subscribe(feedback =>
                {
                    var lines = feedback.Split("\n");
                    parsedFeedbackSubject.OnNext(ParseFeedback(lines));
                });

            const int commandPort = 56888;
            commandStream = new TcpClient(hostname, commandPort);
            Feedback = "Connected.\n";

            frameReceiver = new UdpClient();
            frameReceiver.Client.ReceiveBufferSize = 2 * 1024 * 1024;

            var commandStreamEP = (IPEndPoint)commandStream.Client.LocalEndPoint;
            Serilog.Log.Information($"commandStreamEP={commandStreamEP}");

            frameReceiver.Client.Bind(new IPEndPoint(commandStreamEP.Address, 0));

            Serilog.Log.Information(
                $"frameReceiver bound to local endpoint: {frameReceiver.Client.LocalEndPoint}");

            Task.Run(() => ReceiveFramePackets());

            synchronizationContext = SynchronizationContext.Current;
            Task.Run(() => ReadAndPostFeedback());

            InitializeConnection();

            static ParsedFeedbackFromSonar ParseFeedback(string[] lines)
            {
                var (resultCode, resultString) = ParseResult(lines);

                uint settingsCookie = 0;
                if (resultCode == 200)
                {
                    settingsCookie = ParseSettingsCookie(lines);
                }

                return new ParsedFeedbackFromSonar
                {
                    RawFeedback = String.Join("\n", lines),
                    ResultCode = resultCode,
                    ResultString = resultString,
                    SettingsCookie = settingsCookie,
                };

                (uint resultCode, string resultString) ParseResult(string[] lines)
                {
                    // This is not high-quality parsing. You can do better.
                    var line = lines[0];
                    var parts = line.Split(" ");
                    var code = uint.Parse(parts[0]);
                    var s = parts[1];
                    return (code, s);
                }

                uint ParseSettingsCookie(string[] lines)
                {
                    // This is not high-quality parsing. You can do better.
                    var line = lines[1];
                    var parts = line.Split(" ");

                    if (parts[0] != "settings-cookie")
                    {
                        throw new Exception($"Unexpected value on the second line :{parts[0]}");
                    }

                    return uint.Parse(parts[1]);
                }
            }
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
            catch (ObjectDisposedException ex)
            {
                // Socket was closed.
                Serilog.Log.Information($"Frame receiver bailed: {ex.Message}");
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
                new CommandRequest(
                    "initialize",
                    "salinity brackish",
                    "datetime " + ArisDatetime.GetTimestamp(),
                    $"rcvr_port {frameReceiverPort}"
                    )
                );
        }

        public void StartTestPattern()
        {
            SendCommand(
                commandStream.Client,
                new CommandRequest("testpattern"));
        }

        public void StartPassiveMode()
        {
            SendCommand(
                commandStream.Client,
                new CommandRequest("passive"));
        }

        public void StartDefaultAcquireMode()
        {
            SendCommand(
                commandStream.Client,
                new CommandRequest(
                    "acquire",
                    "start_range 1",
                    "end_range 5")
                );
        }

        public ParsedFeedbackFromSonar StartAcquire(AcquireSettings settings)
        {
            return
                SendCommand(
                    commandStream.Client,
                    new CommandRequest(
                        "acquire",
                        $"start_range {settings.StartRange}",
                        $"end_range {settings.EndRange}")
                    );
        }

        public Frame? WaitOnAFrame(
            SynchronizationContext uiSyncContext,
            Predicate<Frame> predicate,
            CancellationToken ct)
        {
            Frame? receivedFrame = null;

            var timeout = TimeSpan.FromSeconds(2);
            var observation =
                Frames
                    .Where(frame => predicate(frame))
                    .FirstOrDefaultAsync()
                    .ObserveOn(uiSyncContext);

            using (var timeoutCancellation = new CancellationTokenSource(timeout))
            using (var doneSignal = new ManualResetEventSlim())
            {
                observation.Subscribe(
                    frame =>
                    {
                        Interlocked.Exchange(ref receivedFrame, frame);
                        doneSignal.Set();
                    },
                    timeoutCancellation.Token);

                doneSignal.Wait(timeout);
            }

            return receivedFrame;
        }


        private static string FormatCommand(CommandRequest commandRequest) =>
            string.Join("\n", commandRequest.Text) + "\n\n";

        private static string FormatCommandForLogging(string[] commandLines) =>
            string.Join("\\n\n", commandLines) + "\\n\n\\n\n";

        private ParsedFeedbackFromSonar SendCommand(
            Socket socket,
            CommandRequest commandRequest)
        {
            PostCommandText(commandRequest);

            ParsedFeedbackFromSonar feedback = new ParsedFeedbackFromSonar { };

            using (var doneSignal = new ManualResetEventSlim())
            {
                Action<ParsedFeedbackFromSonar> feedbackHandler =
                    parsedFeedback =>
                    {
                        feedback = parsedFeedback;
                        doneSignal.Set();
                    };

                using (var _ = parsedFeedbackSubject.Subscribe(feedbackHandler))
                {


                    var command = FormatCommand(commandRequest);
                    var commandBytes = Encoding.ASCII.GetBytes(command);
                    socket.Send(commandBytes);

                    if (!doneSignal.Wait(TimeSpan.FromSeconds(2)))
                    {
                        throw new Exception("Didn't receive feedback");
                    }
                }
            }

            return feedback;

            void PostCommandText(CommandRequest commandRequest)
            {
                var commandText =
                    "\n"
                    + "Sending command:\n"
                    + ">>>>>>>>\n"
                    + string.Join("\\n\n", commandRequest.Text)
                    + "\\n\n"
                    + "<<<<<<<<\n";
                PostExplanatoryText(commandText);
            }
        }

        private readonly FrameAccumulator frameAccumulator = new FrameAccumulator();
        private readonly SynchronizationContext? synchronizationContext;
        private readonly Subject<string> feedbackSubject;
        private readonly IDisposable feedbackReceiverSub;
        private readonly Subject<ParsedFeedbackFromSonar> parsedFeedbackSubject;
        private readonly IDisposable parsedFeedbackObserver;

        private byte[] feedbackReceiveBuffer = new byte[4096];
        private string offUIFeedbackAccumulator = "";
        private TcpClient commandStream;
        private UdpClient frameReceiver;
    }
}
