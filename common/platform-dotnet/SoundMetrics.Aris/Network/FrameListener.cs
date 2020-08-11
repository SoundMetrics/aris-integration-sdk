using SoundMetrics.Aris.Data;
using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Network
{
    internal sealed class FrameListener : IDisposable
    {
        public FrameListener(IPAddress ipAddress)
        {
            udpListener = new UdpListener(
                address: ipAddress,
                port: -1,
                reuseAddress: false);
            packetSub = udpListener.Packets.Subscribe(OnPacketReceived);
        }

        public IObservable<Frame> Frames => frameSubject;

        private void OnPacketReceived(UdpReceived udpReceived)
        {
            var buffer = udpReceived.Received.Buffer;

            if (buffer.Length <= FramePacketHeaderSize)
            {
                // ignore
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
                        }
                        else
                        {
                            // Can't get frame header, ignore.
                        }
                    }
                    else
                    {
                        // Bad size, ignore.
                    }
                }
                else
                {
                    if (frameAssembler.AddFramePart(packetHeader.PartNumber, payload)
                        && frameAssembler.GetFullFrame(out var frame))
                    {
                        Debug.Assert(!(frame is null));
                        frameSubject.OnNext(frame);
                    }
                }
            }
            else
            {
                // Can't get the packet header, ignore.
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    udpListener.Dispose();

                    packetSub.Dispose();
                    frameSubject.OnCompleted();
                    frameSubject.Dispose();
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
        private readonly Subject<Frame> frameSubject = new Subject<Frame>();
        private readonly FrameAssembler frameAssembler = new FrameAssembler();

        private bool disposed;
    }
}
