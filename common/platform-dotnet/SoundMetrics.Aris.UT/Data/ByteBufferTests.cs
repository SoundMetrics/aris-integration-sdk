using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Data;
using System;

namespace SoundMetrics.Aris
{
    [TestClass]
    public sealed class ByteBufferTests
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
                () => ByteBuffer.Create(-1, InitializeTo42));
        }

        [TestMethod]
        public void EmptyBuffer()
        {
            var buffer = ByteBuffer.Create(0, InitializeTo42);
            Assert.AreEqual(0, buffer.Length);
            Assert.AreEqual(0, buffer.Span.Length);
        }

        [TestMethod]
        public void SingletonBuffer()
        {
            var expected = (byte)(new Random().Next(0, 255));
            var buffer = ByteBuffer.Create(1, buf => InitializeTo(buf, expected));
            var actual = buffer.Span[0];

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public unsafe void SingletonBufferUnsafe()
        {
            var expected = (byte)(new Random().Next(0, 255));
            var buffer = ByteBuffer.Create(1, (buf, length) => InitializeToUnsafe(buf, length, expected));
            var actual = buffer.Span[0];

            Assert.AreEqual(expected, actual);
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
            var buffer1 = ByteBuffer.Create(8, InitializeTo42);
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
