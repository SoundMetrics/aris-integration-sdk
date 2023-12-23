using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data
{
    /// <summary>
    /// Implements a buffer in native heap so it doesn't live
    /// on the Large Object Heap (LOH). Most frames' sample size
    /// dictates that it would need to live in LOH if it were
    /// allocated as managed memory. Allocating on the native heap
    /// instead avoids the overhead of thrashing the LOH.
    /// </summary>
    public sealed class ByteBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        public delegate void InitializeBufferFn(Span<byte> buffer);
        public delegate void TransformBufferFn(
            ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);

        public ByteBuffer(int length, InitializeBufferFn initializeBuffer)
            : base(ownsHandle: true)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    $"{nameof(length)} must be greater than zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            this.length = length;
            base.SetHandle(Marshal.AllocHGlobal(length));

            unsafe
            {
                var writeableBuffer =
                    new Span<byte>(DangerousGetHandle().ToPointer(), length);
                initializeBuffer(writeableBuffer);
            }
        }

        /// <summary>
        /// Construct from existing memory.
        /// </summary>
        public ByteBuffer(ReadOnlyMemory<byte> source)
            : this(source.Length, CreateInitializer(source))
        {
        }

        /// <summary>
        /// Build from an existing buffer. Generally not prefered.
        /// </summary>
        internal ByteBuffer(IntPtr buffer, int bufferLength)
            : base(ownsHandle: true)
        {
            if (bufferLength < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bufferLength),
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    $"{nameof(bufferLength)} must be greater than zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            this.length = bufferLength;
            this.SetHandle(buffer);
        }

        /// <summary>
        /// Construct from a list of buffers.
        /// </summary>
        internal ByteBuffer(List<ReadOnlyMemory<byte>> sourceBuffers)
            : this(SumBufferLengths(sourceBuffers), CreateInitializer(sourceBuffers))
        {
        }

        private static int SumBufferLengths(List<ReadOnlyMemory<byte>> buffers) =>
            buffers.Sum(buffer => buffer.Length);

        private static InitializeBufferFn CreateInitializer(ReadOnlyMemory<byte> source)
        {
            return output =>
            {
                source.Span.CopyTo(output);
            };
        }

        private static InitializeBufferFn CreateInitializer(
            List<ReadOnlyMemory<byte>> sourceBuffers)
        {
            return output =>
            {
                int offset = 0;
                foreach (var buffer in sourceBuffers)
                {
                    var dest = output.Slice(offset, buffer.Length);
                    buffer.Span.CopyTo(dest);
                    offset += buffer.Length;
                }
            };
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(DangerousGetHandle());
            return true;
        }

        public int Length => length;

        public ReadOnlySpan<byte> Span
        {
            get
            {
                unsafe
                {
                    return new ReadOnlySpan<byte>(
                        DangerousGetHandle().ToPointer(), length);
                }
            }
        }

        public ByteBuffer Transform(TransformBufferFn transformBuffer)
        {
            void initialize(Span<byte> output) => transformBuffer(this.Span, output);

            var newBuffer = new ByteBuffer(length, initialize);
            return newBuffer;
        }

        private readonly int length;
    }
}
