﻿//#define SHOW_FRAME_ACCUMULATOR_DEBUG_LOG

using SoundMetrics.Aris.Headers;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    using static SerializationHelpers;

    public class FrameAccumulator
    {
        [System.Diagnostics.Conditional("SHOW_FRAME_ACCUMULATOR_DEBUG_LOG")]
        public void Log(string format, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine("FrameAccumulator: " + format, args);
        }

        public void ReceivePacket(byte[] packet)
        {
            ReceiveRawPacket(new ArraySegment<byte>(packet));
        }

        public void ReceiveRawPacket(ArraySegment<byte> packet)
        {
            var packetHeader = FramePacketHeaderExtensions.FromBytes(packet);
            if (packetHeader.HasValue)
            {
                var payload =
                    new ArraySegment<byte>(
                        packet.Array,
                        PacketHeaderSize,
                        (int)packetHeader.Value.PayloadSize);

                ReceivePacketPayload(packetHeader.Value, payload);
            }
            else
            {
                // Invalid packet header
                Reset();
            }
        }

        private void ReceivePacketPayload(
            in FramePacketHeader packetHeader,
            in ArraySegment<byte> payload)
        {
            Log($"ReceivePacketPayload: frameIndex {packetHeader.FrameIndex}; "
                + $"partNumber {packetHeader.PartNumber}; "
                + $"payload size {payload.Count}");

            var expectedPayloadSize =
                packetHeader.PartNumber == 0
                    ? (uint)Marshal.SizeOf<ArisFrameHeader>()
                    : packetHeader.PayloadSize;

            if (expectedPayloadSize != payload.Count)
            {
                Log($"Malformed packet: payload_size {packetHeader.PayloadSize} doesn't match; "
                    + $"expected payload size is {expectedPayloadSize}");
                return;
            }

            if (packetHeader.PartNumber == 0)
            {
                var newFrameHeader = StructFromBytes<ArisFrameHeader>(payload);
                if (newFrameHeader.HasValue)
                {
                    var isValid = ValidateFrameHeader(newFrameHeader.Value);
                    if (isValid)
                    {
                        Log($"FrameAccumulator: Starting frame {newFrameHeader.Value.FrameIndex}");
                        if (nativeBuffer != null)
                        {
                            Log("Abandoning previous frame");
                        }

                        Reset(packetHeader.FrameIndex, packetHeader.FrameSize);
                        frameHeader = newFrameHeader.Value;

                        nativeBuffer = new NativeBuffer((int)packetHeader.FrameSize);
                    }
                    else
                    {
                        // Ignore someone's packet
                        Log("Part 0 has invalid frame header");
                    }
                }
                else
                {
                    // Invalid frame header in first packet
                    Log("Part 0 has incomplete frame header");
                    Reset();
                }

            }
            else if (packetHeader.FrameIndex != wip.FrameIndex)
            {
                Log("Frame index skipped; resetting");
                Reset();
            }
            else if (packetHeader.PartNumber == wip.GetNextPartNumber())
            {
                Log($"Adding samples to frame {packetHeader.FrameIndex}/{packetHeader.PartNumber}");

                if (wip.AddSamples(packetHeader.PartNumber, payload, nativeBuffer)
                        == FrameCompletion.CompleteFrame)
                {
                    Log($"Completed frame {packetHeader.FrameIndex}");

                    var frame = PackageFrame();

                    // We are not on the UI thread, but frame listeners are on
                    // the UI thread, and callbacks happen asynchronously, so
                    // don't dispose the native buffer, just clear our reference
                    // to it.
                    nativeBuffer = null;

                    frameSubject.OnNext(frame);
                    Reset();
                }
            }
            else
            {
                Log($"Ignoring duplicate part frame index {packetHeader.FrameIndex}; part {packetHeader.PartNumber}");
            }
        }

        public void Reset()
        {
            wip = new WorkInProgress();
            nativeBuffer = null;
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
            frameHeader.FrameIndex = nextFrameIndex++;

            return new Frame
            {
                Header = frameHeader,
                Samples = nativeBuffer,
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
        private NativeBuffer nativeBuffer;
        private ArisFrameHeader frameHeader;
        private uint nextFrameIndex = 0;

        private enum FrameCompletion { IncompleteFrame, CompleteFrame };

        private struct WorkInProgress
        {
            public uint? CurrentPartNumber;
            public uint? FrameIndex;
            public uint ExpectedSampleCount;
            public int SamplesReceived;

            public uint GetNextPartNumber() =>
                CurrentPartNumber.HasValue ? CurrentPartNumber.Value + 1 : 0;


            public FrameCompletion AddSamples(
                uint partNumber,
                ArraySegment<byte> samples,
                NativeBuffer nativeBuffer)
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

                nativeBuffer.Append(samples);
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
