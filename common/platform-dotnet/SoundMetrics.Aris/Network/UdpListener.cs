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
            IPAddress address,
            int port,
            bool reuseAddress)
        {
            LocalEndPoint = GetSafeLocalEndPoint(udp);

            udp.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                reuseAddress);

            try
            {
                var localEndpoint = new IPEndPoint(address, port);
                udp.Client.Bind(localEndpoint);
            }
            catch (Exception ex)
            {
                Log.Error("Binding failed on {address}:{port}; {message}",
                    address, port, ex.Message);
                throw;
            }

            Task.Run(() => Listen(port));
        }


        private static IPEndPoint GetSafeLocalEndPoint(UdpClient udp)
        {
            ArgumentNullException.ThrowIfNull(udp);
            Debug.Assert(udp.Client is not null);

            var ep = udp.Client.LocalEndPoint;
            return ep is null ? new IPEndPoint(IPAddress.Loopback, 42) : (IPEndPoint)ep;
        }

        public IPEndPoint LocalEndPoint { get; }

        public IObservable<UdpReceived> Packets => receivedSubject;

        private async void Listen(int localPort)
        {
            bool keepGoing = true;

            while (keepGoing)
            {
                try
                {
                    var received = await udp.ReceiveAsync().ConfigureAwait(true);
                    var timestamp = DateTimeOffset.Now;
                    ReceivePacket(new UdpReceived(timestamp, received, localPort));
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
                    udp.Dispose();

                    doneSignal.WaitOne();
                    doneSignal.Dispose();

                    receivedSubject.OnCompleted();
                    receivedSubject.Dispose();
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

        private readonly UdpClient udp = new UdpClient();
        private readonly ManualResetEvent doneSignal = new ManualResetEvent(false);
        private readonly Subject<UdpReceived> receivedSubject = new Subject<UdpReceived>();
        private bool disposed;
    }
}
