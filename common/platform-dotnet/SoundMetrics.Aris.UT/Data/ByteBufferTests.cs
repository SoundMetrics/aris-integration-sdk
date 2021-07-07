using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Data;
using System;

namespace SoundMetrics.Aris.UT
{
    [TestClass]
    public sealed class ByteBufferTests
    {
        private void InitializeTo42(Span<byte> buffer)
        {
            InitializeTo(buffer, 42);
        }

        private void InitializeTo(Span<byte> buffer, byte value)
        {
            for (int index = 0; index < buffer.Length; ++index)
            {
                buffer[index] = value;
            }
        }

        [TestMethod]
        public void NegativeSizedBuffer()
        {
            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new ByteBuffer(-1, InitializeTo42));
        }

        [TestMethod]
        public void EmptyBuffer()
        {
            var buffer = new ByteBuffer(0, InitializeTo42);
            Assert.AreEqual(0, buffer.Length);
            Assert.AreEqual(0, buffer.Span.Length);
        }

        [TestMethod]
        public void SingletonBuffer()
        {
            var expected = (byte)(new Random().Next(0, 255));
            var buffer = new ByteBuffer(1, buf => InitializeTo(buf, expected));
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
            var buffer1 = new ByteBuffer(8, InitializeTo42);
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
