using Aris.FileTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol.UT
{
    using static SerializationHelpers;

    [TestClass]
    public class FrameAccumulatorTests
    {
        #region Helpers

        private static void AssertArraysAreEqual<T>(T[] a, T[] b)
            where T : IEquatable<T>
        {
            if (a.Length != b.Length)
            {
                Assert.Fail("Arrays are different lengths");
            }

            for (int i = 0; i < a.Length; ++i)
            {
                if (!a[i].Equals(b[i]))
                {
                    Assert.Fail($"Element at index {i} differs");
                }
            }

            Assert.IsTrue(true);
        }

        private struct GeneratedFrameInfo
        {
            public ArisFrameHeader FrameHeader;
            public byte[] Samples;
        }

        private static GeneratedFrameInfo ApplyValidFrame(
            FrameAccumulator fa,
            uint frameIndex,
            uint sampleCount
        )
        {
            if (sampleCount < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sampleCount), "test with more than two samples");
            }

            var (firstPacketBytes, frameHeader) =
                GenerateFirstPacket(frameIndex, sampleCount);

            fa.ReceivePacket(firstPacketBytes);

            var (samplePackets, sampleData) =
                GenerateSamplePackets(frameIndex, sampleCount);

            foreach (var packet in samplePackets)
            {
                fa.ReceivePacket(packet);
            }

            return new GeneratedFrameInfo
            {
                FrameHeader = frameHeader,
                Samples = sampleData,
            };
        }

        private static (byte[], ArisFrameHeader) GenerateFirstPacket(
            uint frameIndex,
            uint sampleCount
        )
        {
            var packetHeader = new FramePacketHeader
            {
                Signature = FramePacketHeader.ExpectedSignature,
                HeaderSize = (uint)Marshal.SizeOf<FramePacketHeader>(),
                FrameSize = sampleCount,
                FrameIndex = frameIndex,
                PartNumber = 0,
            };

            var frameHeader = new ArisFrameHeader
            {
                Version = (uint)ArisFrameHeader.ArisFrameSignature,
                PingMode = 9,
                SamplesPerBeam = 20,
            };

            return
                (BytesFromStruct(packetHeader)
                    .Concat(BytesFromStruct(frameHeader))
                    .ToArray(),
                 frameHeader);

        }

        private static ArraySegment<byte>
            GenerateSamplePacket(
                uint frameIndex,
                uint partNumber,
                ArraySegment<byte> samples)
        {
            var packetHeader = new FramePacketHeader
            {
                Signature = FramePacketHeader.ExpectedSignature,
                HeaderSize = (uint)Marshal.SizeOf<FramePacketHeader>(),
                FrameSize = (uint)samples.Count,
                FrameIndex = frameIndex,
                PartNumber = partNumber,
            };

            return BytesFromStruct(packetHeader)
                .Concat(samples.ToArray())
                .ToArray();
        }

        private static (IEnumerable<ArraySegment<byte>>, byte[])
            GenerateSamplePackets(uint frameIndex, uint totalSampleCount)
        {
            var samples = GenerateSamples();
            var packets = GeneratePackets();

            return (packets, samples);

            byte[] GenerateSamples()
            {
                var buffer = new byte[totalSampleCount];
                byte value = 0;

                for (int i = 0; i < totalSampleCount; ++i)
                {
                    buffer[i] = value++;
                }

                return buffer;
            }

            IEnumerable<ArraySegment<byte>> GeneratePackets()
            {
                var firstHalf = (int)(totalSampleCount / 2);
                var secondHalf = (int)(totalSampleCount - firstHalf);

                yield return GenerateSamplePacket(
                    frameIndex,
                    partNumber: 1u,
                    samples: new ArraySegment<byte>(samples, 0, firstHalf));
                yield return GenerateSamplePacket(
                    frameIndex,
                    partNumber: 2u,
                    samples: new ArraySegment<byte>(samples, firstHalf, secondHalf));
            }
        }

        #endregion Helpers

        [TestMethod]
        public void GoodFrameTest()
        {
            Frame frame = null;
            int frameCount = 0;
            var fa = new FrameAccumulator();

            using (var _ = fa.Frames.Subscribe(newFrame =>
            {
                frame = newFrame;
                ++frameCount;
            }))
            {
                var frameInfo =
                    ApplyValidFrame(fa, frameIndex: 42, sampleCount: 24);

                Assert.AreEqual(1, frameCount);
                Assert.IsNotNull(frame);

                AssertArraysAreEqual(
                    BytesFromStruct(frameInfo.FrameHeader),
                    BytesFromStruct(frame.Header)
                    );
                // This assertion fails with the following message:
                //      'Assert.AreEqual failed.
                //          Expected:<Aris.FileTypes.ArisFrameHeader>.
                //            Actual:<Aris.FileTypes.ArisFrameHeader>.'
                // This seems a bit nonsensical, as the values prove to be identical,
                // so we're using AssertArraysAreEqual, above.
                //Assert.AreEqual<ArisFrameHeader>(frameInfo.FrameHeader, frame.Header);

                AssertArraysAreEqual<byte>(
                    frameInfo.Samples,
                    frame.Samples.ToManagedArray());
            }
        }

        [TestMethod]
        public void FrameHeaderOnly()
        {
            Frame frame = null;
            int frameCount = 0;
            var fa = new FrameAccumulator();

            using (var _ = fa.Frames.Subscribe(newFrame =>
            {
                frame = newFrame;
                ++frameCount;
            }))
            {
                var (firstSoloPacket, _) =
                    GenerateFirstPacket(frameIndex: 0, sampleCount: 24);
                fa.ReceivePacket(firstSoloPacket);

                var frameInfo =
                    ApplyValidFrame(fa, frameIndex: 42, sampleCount: 24);

                Assert.AreEqual(1, frameCount);
                Assert.IsNotNull(frame);

                AssertArraysAreEqual(
                    BytesFromStruct(frameInfo.FrameHeader),
                    BytesFromStruct(frame.Header)
                    );
                // This assertion fails with the following message:
                //      'Assert.AreEqual failed.
                //          Expected:<Aris.FileTypes.ArisFrameHeader>.
                //            Actual:<Aris.FileTypes.ArisFrameHeader>.'
                // This seems a bit nonsensical, as the values prove to be identical,
                // so we're using AssertArraysAreEqual, above.
                //Assert.AreEqual<ArisFrameHeader>(frameInfo.FrameHeader, frame.Header);

                AssertArraysAreEqual<byte>(
                    frameInfo.Samples,
                    frame.Samples.ToManagedArray());
            }
        }

        [TestMethod]
        public void FrameHeaderAndIncompleteSamplesOnly()
        {
            Frame frame = null;
            int frameCount = 0;
            var fa = new FrameAccumulator();

            using (var _ = fa.Frames.Subscribe(newFrame =>
            {
                frame = newFrame;
                ++frameCount;
            }))
            {
                // Send the fram header packet.
                var (firstSoloPacket, _) = GenerateFirstPacket(0, 24);
                fa.ReceivePacket(firstSoloPacket);

                // Send incomplete samples.
                var (packets, _) = GenerateSamplePackets(0, 24);
                var firstSamplePacketOnly = packets.First();
                fa.ReceivePacket(firstSamplePacketOnly);

                // Send a valid frame.
                var frameInfo =
                    ApplyValidFrame(fa, frameIndex: 42, sampleCount: 24);

                Assert.AreEqual(1, frameCount);
                Assert.IsNotNull(frame);

                AssertArraysAreEqual(
                    BytesFromStruct(frameInfo.FrameHeader),
                    BytesFromStruct(frame.Header)
                    );
                // This assertion fails with the following message:
                //      'Assert.AreEqual failed.
                //          Expected:<Aris.FileTypes.ArisFrameHeader>.
                //            Actual:<Aris.FileTypes.ArisFrameHeader>.'
                // This seems a bit nonsensical, as the values prove to be identical,
                // so we're using AssertArraysAreEqual, above.
                //Assert.AreEqual<ArisFrameHeader>(frameInfo.FrameHeader, frame.Header);

                AssertArraysAreEqual<byte>(
                    frameInfo.Samples,
                    frame.Samples.ToManagedArray());
            }
        }

        [TestMethod]
        public void DuplicateSamplePackets()
        {
            Frame frame = null;
            int frameCount = 0;
            var fa = new FrameAccumulator();

            using (var _ = fa.Frames.Subscribe(newFrame =>
            {
                frame = newFrame;
                ++frameCount;
            }))
            {
                // Send the fram header packet.
                var (firstPacket, _) = GenerateFirstPacket(0, 24);
                Debug.WriteLine("  Sending first packet");
                fa.ReceivePacket(firstPacket);

                // Send duplicate samples.
                var (packets, _) = GenerateSamplePackets(0, 24);
                var doubledPackets =
                    packets.SelectMany(packet => new[] { packet, packet });
                foreach (var packet in doubledPackets)
                {
                    Debug.WriteLine($"  Sending sample packet");
                    fa.ReceivePacket(packet);
                }

                // Send a valid frame.
                var frameInfo =
                    ApplyValidFrame(fa, frameIndex: 86, sampleCount: 24);

                // Should receive our first frame constructed from duplicates,
                // followed by our second, normal frame.
                Assert.AreEqual(2, frameCount);
                Assert.IsNotNull(frame);

                AssertArraysAreEqual(
                    BytesFromStruct(frameInfo.FrameHeader),
                    BytesFromStruct(frame.Header)
                    );
                // This assertion fails with the following message:
                //      'Assert.AreEqual failed.
                //          Expected:<Aris.FileTypes.ArisFrameHeader>.
                //            Actual:<Aris.FileTypes.ArisFrameHeader>.'
                // This seems a bit nonsensical, as the values prove to be identical,
                // so we're using AssertArraysAreEqual, above.
                //Assert.AreEqual<ArisFrameHeader>(frameInfo.FrameHeader, frame.Header);

                AssertArraysAreEqual<byte>(
                    frameInfo.Samples,
                    frame.Samples.ToManagedArray());
            }
        }
    }
}
