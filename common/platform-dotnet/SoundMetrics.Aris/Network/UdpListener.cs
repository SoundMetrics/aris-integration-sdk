using Serilog;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SoundMetrics.Aris.Network
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct UdpReceived
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public UdpReceived(
            DateTimeOffset timestamp,
            UdpReceiveResult received,
            int localPort)
        {
            Timestamp = timestamp;
            Received = received;
            LocalPort = localPort;
        }

#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly DateTimeOffset Timestamp;
        public readonly UdpReceiveResult Received;
        public readonly int LocalPort;
#pragma warning restore CA1051 // Do not declare visible instance fields
    }

    internal sealed class UdpListener : IDisposable
    {
        public UdpListener(
            IPAddress listeningAddress,
            int port,
            bool reuseAddress,
            string context)
        {
            this.context = context;

            udp.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                reuseAddress);

            try
            {
                var localEndpoint = new IPEndPoint(listeningAddress, port);
                udp.Client.Bind(localEndpoint);
                if (udp.Client.LocalEndPoint is IPEndPoint ep)
                {
                    LocalEndPoint = ep;
                    Log.Information("Bound {type} to {ep} ({context})",
                        nameof(UdpListener), ep, context);
                }
                else
                {
                    throw new Exception($"Couldn't get local endpoint on {localEndpoint} for {nameof(UdpListener)}");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Binding failed on {address}:{port}; {message}",
                    listeningAddress, port, ex.Message);
                throw;
            }

            Task.Run(() => Listen(port));
        }

        public IPEndPoint LocalEndPoint { get; }

        public IObservable<UdpReceived> Packets => receivedSubject;

        private async void Listen(int localPort)
        {
            bool keepGoing = true;
            CancellationToken ct = cts.Token;

            while (keepGoing && !ct.IsCancellationRequested)
            {
                try
                {
                    var received = await udp.ReceiveAsync(ct).ConfigureAwait(true);
                    var timestamp = DateTimeOffset.Now;
                    ReceivePacket(new UdpReceived(timestamp, received, localPort));
                }
                catch (OperationCanceledException)
                {
                    // The cancellation token is cancelled.
                    Log.Information("{function}: UdpListener was cancelled ({context})", nameof(Listen), context);
                    keepGoing = false; // Redundant, but okay.
                }
                catch (ObjectDisposedException)
                {
                    // Somebody disposed us.
                    keepGoing = false;
                }
                catch (SocketException)
                {
                    // Socket was shut down.
                    // SocketException (10058): A request to send or receive
                    // data was disallowed because the socket had already
                    // been shut down in that direction with a previous
                    // shutdown call.
                    keepGoing = false;
                }
            }

            doneSignal.Set();
        }

        private void ReceivePacket(in UdpReceived received)
        {
            if (receivedSubject.HasObservers)
            {
                receivedSubject.OnNext(received);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    cts.Cancel();

                    udp.Dispose();

                    doneSignal.WaitOne();
                    doneSignal.Dispose();

                    receivedSubject.OnCompleted();
                    receivedSubject.Dispose();

                    cts.Dispose();
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

        private readonly string context;
        private readonly UdpClient udp = new UdpClient();
        private readonly ManualResetEvent doneSignal = new ManualResetEvent(false);
        private readonly Subject<UdpReceived> receivedSubject = new Subject<UdpReceived>();
        private readonly CancellationTokenSource cts = new();

        private bool disposed;
    }
}
