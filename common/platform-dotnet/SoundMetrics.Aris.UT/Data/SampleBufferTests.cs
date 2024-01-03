using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Data;
using System;

namespace SoundMetrics.Aris
{
    [TestClass]
    public sealed class SampleBufferTests
    {
        private static void InitializeTo42(Span<byte> buffer)
        {
            InitializeTo(buffer, 42);
        }

        private static void InitializeTo(Span<byte> buffer, byte value)
        {
            buffer.Fill(value);
        }

        private static unsafe void InitializeToUnsafe(byte* buffer, int length, byte value)
        {
            new Span<byte>(buffer, length).Fill(value);
        }

        [TestMethod]
        public void NegativeSizedBuffer()
        {
            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => SampleBuffer.Create(-1, InitializeTo42));
        }

        [TestMethod]
        public void EmptyBuffer()
        {
            var buffer = SampleBuffer.Create(0, InitializeTo42);
            Assert.AreEqual(0, buffer.Length);
            Assert.AreEqual(0, buffer.Span.Length);
        }

        [TestMethod]
        public void SingletonBuffer()
        {
            var expected = (byte)(new Random().Next(0, 255));
            var buffer = SampleBuffer.Create(1, buf => InitializeTo(buf, expected));
            var actual = buffer.Span[0];

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public unsafe void SingletonBufferUnsafe()
        {
            var expected = (byte)(new Random().Next(0, 255));
            var buffer = SampleBuffer.Create(1, (buf, length) => InitializeToUnsafe(buf, length, expected));
            var actual = buffer.Span[0];

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void FailAllocation()
        {
            const int OversizedBufferSize = int.MaxValue; // Successful test depends on the UT assembly being 32-bit.
            _ = Assert.ThrowsException<OutOfMemoryException>(
                () => SampleBuffer.Create(OversizedBufferSize, InitializeTo42));
        }

        [TestMethod]
        public void CopyBuffer()
        {
            ReadOnlySpan<byte> source = new byte[] { 1, 2, 3 };
            var expected = source;
            var actual = SampleBuffer.Create(source);

            CollectionAssert.AreEqual(expected.ToArray(), actual.Span.ToArray());
        }

        [TestMethod]
        public void CopyMultipleBuffers()
        {
            var sources = new[]
            {
                new ReadOnlyMemory<byte>(new byte[] { 1, 2 }),
                new ReadOnlyMemory<byte>(new byte[] { 3, 4 }),
            };
            ReadOnlySpan<byte> expected = new byte[] { 1, 2, 3, 4 };
            var actual = SampleBuffer.Create(sources);

            CollectionAssert.AreEqual(expected.ToArray(), actual.Span.ToArray());
        }

        [TestMethod]
        public void Transform()
        {
            void TransformFn(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
            {
                for (int index = 0; index < inputBuffer.Length; ++index)
                {
                    outputBuffer[index] = (byte)(2 * inputBuffer[index]);
                }
            }

            var expected = 42 * 2;
            var buffer1 = SampleBuffer.Create(8, InitializeTo42);
            var buffer2 = buffer1.Transform(TransformFn);

            Assert.IsNotNull(buffer2);
            Assert.AreEqual(buffer1.Length, buffer2.Length);

            for (int index = 0; index < buffer2.Length; ++index)
            {
                Assert.AreEqual(
                    expected,
                    buffer2.Span[index],
                    $"Unexpected value at index {index}");
            }
        }
    }
}
