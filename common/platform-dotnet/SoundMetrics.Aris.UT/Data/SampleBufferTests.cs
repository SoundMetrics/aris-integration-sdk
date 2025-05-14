using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using System;
using System.Linq;
using Linq = System.Linq;

namespace SoundMetrics.Aris
{
    [TestClass]
    public sealed class SampleBufferTests
    {
        private static readonly SampleGeometry dummySampleGeometry = SampleGeometry.Invalid;
        private static readonly FrameHeaderRef dummyFrameHeaderRef = new(new());

        private static void InitializeTo42(SampleGeometry sampleGeometry, Span<byte> buffer)
        {
            InitializeTo(sampleGeometry, buffer, 42);
        }

        private static void InitializeTo(SampleGeometry sampleGeometry, Span<byte> buffer, byte value)
        {
            buffer.Fill(value);
        }

        private static unsafe void InitializeToUnsafe(SampleGeometry sampleGeometry, IntPtr buffer, int length, byte value)
        {
            new Span<byte>(buffer.ToPointer(), length).Fill(value);
        }

        private static unsafe void InitializeToUnsafe(SampleGeometry sampleGeometry, IntPtr buffer, int length, Span<byte> values)
        {
            values.Slice(0, length).CopyTo(new Span<byte>(buffer.ToPointer(), length));
        }

        [TestMethod]
        public void NegativeSizedBuffer()
        {
            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => ReadOnlySampleBuffer.Create(dummySampleGeometry, -1, InitializeTo42));
        }

        [TestMethod]
        public void EmptyBuffer()
        {
            var buffer = ReadOnlySampleBuffer.Create(dummySampleGeometry, 0, InitializeTo42);
            Assert.AreEqual(0, buffer.Length);
            Assert.AreEqual(0, buffer.Span.Length);
        }

        [TestMethod]
        public void SingletonBuffer()
        {
            var expected = (byte)(new Random().Next(0, 255));
            var buffer = ReadOnlySampleBuffer.Create(
                dummySampleGeometry,
                1,
                (sampleGeometry, buf) => InitializeTo(sampleGeometry, buf, expected));
            var actual = buffer.Span[0];

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public unsafe void SingletonBufferUnsafe()
        {
            byte expected = 0x7e;
            var buffer = 
                ReadOnlySampleBuffer.Create(
                    dummySampleGeometry,
                    1,
                    (sampleGeometry, buf, length) => InitializeToUnsafe(sampleGeometry, buf, length, expected));
            var actual = buffer.Span[0];

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public unsafe void SingletonBufferUnsafe2()
        {
            int bufferLength = 3;
            byte[] initialValue = Linq.Enumerable.Range(101, 100).Select(i => (byte)i).ToArray();
            var buffer = ReadOnlySampleBuffer.Create(
                dummySampleGeometry,
                bufferLength,
                (sampleGeometry, buf, length) => InitializeToUnsafe(sampleGeometry, buf, length, initialValue));
            var expected = initialValue[0];
            var actual = buffer.Span[0];

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void FailAllocation()
        {
            const int OversizedBufferSize = int.MaxValue; // Successful test depends on the UT assembly being 32-bit.
            _ = Assert.ThrowsException<OutOfMemoryException>(
                () => ReadOnlySampleBuffer.Create(dummySampleGeometry, OversizedBufferSize, InitializeTo42));
        }

        [TestMethod]
        public void CopyBuffer()
        {
            ReadOnlySpan<byte> source = new byte[] { 1, 2, 3 };
            var expected = source;
            var actual = ReadOnlySampleBuffer.CopyFrom(source);

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
            var actual = ReadOnlySampleBuffer.Create(sources);

            CollectionAssert.AreEqual(expected.ToArray(), actual.Span.ToArray());
        }

        [TestMethod]
        public void Transform()
        {
            void TransformFn(
                FrameHeaderRef frameHeaderRef,
                SampleGeometry sampleGeometry,
                ReadOnlySpan<byte> inputBuffer,
                Span<byte> outputBuffer)
            {
                for (int index = 0; index < inputBuffer.Length; ++index)
                {
                    outputBuffer[index] = (byte)(2 * inputBuffer[index]);
                }
            }

            var expected = 42 * 2;
            var buffer1 = ReadOnlySampleBuffer.Create(dummySampleGeometry, 8, InitializeTo42);
            var buffer2 = buffer1.Transform(dummyFrameHeaderRef, dummySampleGeometry, TransformFn);

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
