using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SoundMetrics.Aris.Network
{
    public struct UdpReceived
    {
        public DateTimeOffset Timestamp;
        public UdpReceiveResult Received;
    }

    internal sealed class UdpListener : IDisposable
    {
        public UdpListener(
            IPAddress address,
            int port,
            bool reuseAddress)
        {
            udp.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                reuseAddress);
            udp.Client.Bind(new IPEndPoint(address, port));

            Task.Run(Listen);
        }

        public IObservable<UdpReceived> Packets => receivedSubject;

        private async void Listen()
        {
            bool keepGoing = true;

            while (keepGoing)
            {
                try
                {
                    var received = await udp.ReceiveAsync();
                    var timestamp = DateTimeOffset.Now;
                    ReceivePacket(new UdpReceived
                    {
                        Timestamp = timestamp,
                        Received = received,
                    });
                }
                catch (ObjectDisposedException)
                {
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
