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
            var packetHeader = FramePacketHeaderExtensions.FromBytes(packet);
            if (packetHeader.HasValue)
            {
                var payload =
                    new ArraySegment<byte>(
                        packet,
                        PacketHeaderSize,
                        packet.Length - PacketHeaderSize);

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
                    Reset(packetHeader.FrameIndex, packetHeader.FrameSize);
                    frameHeader = newFrameHeader.Value;
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
                wip.AddSamples(packetHeader.PartNumber, payload);
                //AccumulateSamples(packetHeader, wip);

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

        public IObservable<object> Frames { get { return frameSubject; } }

        private static readonly int PacketHeaderSize = Marshal.SizeOf<FramePacketHeader>();
        private static readonly int FrameHeaderSize = Marshal.SizeOf<ArisFrameHeader>();
        private readonly Subject<object> frameSubject = new Subject<object>();

        private WorkInProgress wip = new WorkInProgress();
        private ArisFrameHeader frameHeader;

        private struct WorkInProgress
        {
            public uint? CurrentPartNumber;
            public uint? FrameIndex;
            public uint ExpectedSampleCount;
            public List<ArraySegment<byte>> Samples;
            public int SamplesReceived;

            public uint GetNextPartNumber() =>
                CurrentPartNumber.HasValue ? CurrentPartNumber.Value + 1 : 0;


            public void AddSamples(uint partNumber, ArraySegment<byte> samples)
            {
                if (samples.Count == 0)
                {
                    throw new ArgumentException(
                        "empty sample packet",
                        nameof(samples));
                }

                if (partNumber != GetNextPartNumber())
                {
                    return;
                }

                if (Samples == null)
                {
                    Samples = new List<ArraySegment<byte>>();
                }

                Samples.Add(samples);
                SamplesReceived += samples.Count;
                CurrentPartNumber = GetNextPartNumber();
            }

            private bool IsFrameComplete()
            {
                if (ExpectedSampleCount == 0)
                {
                    throw new InvalidOperationException(
                        "Cannot check for complete frame before the frame header is received");
                }

                return SamplesReceived == ExpectedSampleCount;
            }
        }
    }
}
