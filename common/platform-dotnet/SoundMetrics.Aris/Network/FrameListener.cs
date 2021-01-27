using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;

namespace SoundMetrics.Aris.Network
{
    internal sealed class FrameListener : IDisposable
    {
        public FrameListener(IPAddress ipAddress, Subject<Frame> frameSubject)
        {
            this.frameSubject = frameSubject;
            udpListener = new UdpListener(
                address: ipAddress,
                port: 0,
                reuseAddress: false);
            packetSub = udpListener.Packets.Subscribe(OnPacketReceived);
        }

        public IPEndPoint LocalEndPoint => udpListener.LocalEndPoint;

        public FrameListenerMetrics Metrics
        {
            get { lock (metricsGuard) { return metrics; }; }
        }

        internal IObservable<DateTimeOffset> ValidPacketReceived => validPacketReceived;

        private void OnPacketReceived(UdpReceived udpReceived)
        {
            bool isInvalidPacket = false;
            bool startedFrame = false;
            bool completedFrame = false;

            try
            {
                var buffer = udpReceived.Received.Buffer;

                if (buffer.Length <= FramePacketHeaderSize)
                {
                    // ignore
                    isInvalidPacket = true;
                    return;
                }

                var packetHeaderBytes =
                    (new Span<byte>(buffer)).Slice(0, FramePacketHeaderSize);
                if (packetHeaderBytes.ReadStruct<FramePacketHeader>(out var packetHeader))
                {
                    if (packetHeader.Signature != FramePacketHeader.FramePacketSignature
                        || packetHeader.HeaderSize != FramePacketHeaderSize)
                    {
                        // Not a valid frame packet, ignore.
                        isInvalidPacket = true;
                        return;
                    }

                    var payload =
                        new Memory<byte>(
                            buffer,
                            FramePacketHeaderSize,
                            buffer.Length - FramePacketHeaderSize);

                    if (packetHeader.PartNumber == 0)
                    {
                        // The payload is the frame header
                        if (packetHeader.PayloadSize == FrameHeaderSize
                            && payload.Length == FrameHeaderSize)
                        {
                            if (payload.Span.ReadStruct<FrameHeader>(out var frameHeader))
                            {
                                frameAssembler.SetFrameHeader(frameHeader);
                                startedFrame = true;
                            }
                            else
                            {
                                // Can't get frame header, ignore.
                                isInvalidPacket = true;
                            }
                        }
                        else
                        {
                            // Bad size, ignore.
                            isInvalidPacket = true;
                        }
                    }
                    else
                    {
                        // The frame part is zero-based, and starts in packet
                        // part number 1.
                        var framePart = packetHeader.PartNumber - 1;

                        if (frameAssembler.AddFramePart(framePart, payload)
                            && frameAssembler.GetFullFrame(out var frame))
                        {
                            Debug.Assert(!(frame is null));
                            frameSubject.OnNext(frame);
                            completedFrame = true;
                        }
                    }
                }
                else
                {
                    // Can't get the packet header, ignore.
                    isInvalidPacket = true;
                }
            }
            finally
            {
                var localMetrics = new FrameListenerMetrics
                {
                    PacketsReceived = 1,
                    InvalidPacketsReceived = isInvalidPacket ? 1 : 0,
                    FramesStarted = startedFrame ? 1 : 0,
                    FramesCompleted = completedFrame ? 1 : 0,
                };

                lock (metricsGuard)
                {
                    metrics += localMetrics;
                }

                if (!isInvalidPacket)
                {
                    validPacketReceived.OnNext(udpReceived.Timestamp);
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    validPacketReceived.OnCompleted();
                    validPacketReceived.Dispose();
                    udpListener.Dispose();
                    packetSub.Dispose();
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

        private static readonly int FramePacketHeaderSize = Marshal.SizeOf<FramePacketHeader>();
        private static readonly int FrameHeaderSize = Marshal.SizeOf<FrameHeader>();
        private readonly UdpListener udpListener;
        private readonly IDisposable packetSub;
        private readonly Subject<Frame> frameSubject;
        private readonly FrameAssembler frameAssembler = new FrameAssembler();
        private readonly Subject<DateTimeOffset> validPacketReceived = new Subject<DateTimeOffset>();
        private readonly Mutex metricsGuard = new Mutex();

        private bool disposed;
        private FrameListenerMetrics metrics = new FrameListenerMetrics();
    }
}
