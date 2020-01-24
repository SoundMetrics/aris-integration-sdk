using Aris.FileTypes;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    using static SerializationHelpers;

    public class FrameAccumulator
    {
        public void ReceivePacket(byte[] packet)
        {
            ReceivePacket(new ArraySegment<byte>(packet));
        }

        public void ReceivePacket(ArraySegment<byte> packet)
        {
            var packetHeader = FramePacketHeaderExtensions.FromBytes(packet);
            if (packetHeader.HasValue)
            {
                var payload =
                    new ArraySegment<byte>(
                        packet.Array,
                        PacketHeaderSize,
                        packet.Count - PacketHeaderSize);

                ReceivePacket(packetHeader.Value, payload);
            }
            else
            {
                // Invalid packet header
                Reset();
            }
        }

        private void ReceivePacket(
            in FramePacketHeader packetHeader,
            in ArraySegment<byte> payload)
        {
            if (packetHeader.PartNumber == 0)
            {
                var newFrameHeader = StructFromBytes<ArisFrameHeader>(payload);
                if (newFrameHeader.HasValue)
                {
                    var isValid = ValidateFrameHeader(newFrameHeader.Value);
                    if (isValid)
                    {
                        Reset(packetHeader.FrameIndex, packetHeader.FrameSize);
                        frameHeader = newFrameHeader.Value;
                    }
                    else
                    {
                        // Ignore someone's packet
                    }
                }
                else
                {
                    // Invalid frame header in first packet
                    Reset();
                }

            }
            else if (packetHeader.FrameIndex != wip.FrameIndex)
            {
                Reset();
            }
            else if (packetHeader.PartNumber == wip.GetNextPartNumber())
            {
                if (wip.AddSamples(packetHeader.PartNumber, payload)
                        == FrameCompletion.CompleteFrame)
                {
                    var frame = PackageFrame();
                    frameSubject.OnNext(frame);

                    Reset();
                }
            }
        }

        public void Reset()
        {
            wip = new WorkInProgress();
        }

        private void Reset(uint frameIndex, uint expectedSampleCount)
        {
            wip = new WorkInProgress
            {
                CurrentPartNumber = 0,
                FrameIndex = frameIndex,
                ExpectedSampleCount = expectedSampleCount,
            };
        }

        private Frame PackageFrame()
        {
            return new Frame
            {
                Header = frameHeader,
                Samples = new NativeBuffer(wip.Samples),
            };
        }

        private static bool ValidateFrameHeader(in ArisFrameHeader header)
        {
            return header.Version == ArisFrameHeader.ArisFrameSignature
                && ValidPingModes.Contains(header.PingMode)
                && header.SamplesPerBeam <= 4000;
        }

        private static readonly HashSet<uint> ValidPingModes =
            new HashSet<uint>(new[] { 1u, 3u, 6u, 9u });

        public IObservable<Frame> Frames { get { return frameSubject; } }

        private static readonly int PacketHeaderSize = Marshal.SizeOf<FramePacketHeader>();
        private readonly Subject<Frame> frameSubject = new Subject<Frame>();

        private WorkInProgress wip = new WorkInProgress();
        private ArisFrameHeader frameHeader;

        private enum FrameCompletion { IncompleteFrame, CompleteFrame };

        private struct WorkInProgress
        {
            public uint? CurrentPartNumber;
            public uint? FrameIndex;
            public uint ExpectedSampleCount;
            public List<ArraySegment<byte>> Samples;
            public int SamplesReceived;

            public uint GetNextPartNumber() =>
                CurrentPartNumber.HasValue ? CurrentPartNumber.Value + 1 : 0;


            public FrameCompletion AddSamples(uint partNumber, ArraySegment<byte> samples)
            {
                if (samples.Count == 0)
                {
                    throw new ArgumentException(
                        "empty sample packet",
                        nameof(samples));
                }

                if (partNumber != GetNextPartNumber())
                {
                    return FrameCompletion.IncompleteFrame;
                }

                if (Samples == null)
                {
                    Samples = new List<ArraySegment<byte>>();
                }

                Samples.Add(samples);
                SamplesReceived += samples.Count;
                CurrentPartNumber = GetNextPartNumber();

                return
                    SamplesReceived == ExpectedSampleCount
                    ? FrameCompletion.CompleteFrame
                    : FrameCompletion.IncompleteFrame;
            }
        }
    }
}
