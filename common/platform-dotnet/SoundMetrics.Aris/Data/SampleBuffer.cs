using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data
{
    internal sealed class HGlobalSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public HGlobalSafeHandle(IntPtr buffer)
            : base(ownsHandle: true)
        {
            SetHandle(buffer);
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }

    /// <summary>
    /// Implements a buffer in native heap so it doesn't live
    /// on the Large Object Heap (LOH). Most frames' sample size
    /// dictates that it would need to live in LOH if it were
    /// allocated as managed memory. Allocating on the native heap
    /// instead avoids the overhead of thrashing the LOH.
    /// </summary>
    public sealed class SampleBuffer : IDisposable
    {
        public delegate void InitializeBufferSpan(Span<byte> buffer);
        public delegate void InitializeBuffer(IntPtr buffer, int length);

        public delegate void TransformBufferSpan(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);
        public delegate void TransformBuffer(IntPtr inputBuffer, IntPtr outputBuffer, int length);

        private SampleBuffer(HGlobalSafeHandle handle, IntPtr alignedBuffer, int length)
        {
            this.handle = handle;
            this.alignedBuffer = alignedBuffer;
            this.length = length;
        }

        public static SampleBuffer Create(int length, InitializeBufferSpan initializeBuffer)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    $"{nameof(length)} must be greater than zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            if (initializeBuffer is null)
            {
                throw new ArgumentNullException(nameof(initializeBuffer));
            }

            var sampleBuffer = CreateBuffer(length);

            initializeBuffer(sampleBuffer.WriteableSpan);

            return sampleBuffer;
        }

        public static SampleBuffer Create(int length, InitializeBuffer initializeBuffer)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    $"{nameof(length)} must be greater than zero");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            if (initializeBuffer is null)
            {
                throw new ArgumentNullException(nameof(initializeBuffer));
            }

            var sampleBuffer = CreateBuffer(length);

            initializeBuffer(sampleBuffer.alignedBuffer, length);

            return sampleBuffer;
        }

        public static SampleBuffer Create(ReadOnlySpan<byte> source)
        {
            var sampleBuffer = CreateBuffer(source.Length);

            source.CopyTo(sampleBuffer.WriteableSpan);

            return sampleBuffer;
        }

        /// <summary>
        /// Construct from a list of buffers.
        /// </summary>
        public unsafe static SampleBuffer Create(IEnumerable<ReadOnlyMemory<byte>> sourceBuffers)
        {
            var sources = sourceBuffers.ToArray();
            var totalLength = sources.Sum(source => source.Length);

            var sampleBuffer = CreateBuffer(totalLength);

            var fullDestination = sampleBuffer.WriteableSpan;
            int offset = 0;

            foreach (var source in sources)
            {
                var partialOutput = fullDestination.Slice(offset, source.Length);
                source.Span.CopyTo(partialOutput);
                offset += partialOutput.Length;
            }

            return sampleBuffer;
        }

        public SampleBuffer Transform(TransformBufferSpan transformBuffer)
        {
            void initialize(Span<byte> output) => transformBuffer(this.Span, output);
            var newBuffer = SampleBuffer.Create(length, initialize);
            return newBuffer;
        }

        public unsafe SampleBuffer Transform(TransformBuffer transformBufferUnsafe)
        {
            void initialize(IntPtr outputBuffer, int length)
                => transformBufferUnsafe(alignedBuffer, outputBuffer, length);

            var newBuffer = SampleBuffer.Create(length, initialize);
            return newBuffer;
        }

        public int Length => length;

        public ReadOnlySpan<byte> Span
        {
            get
            {
                unsafe
                {
                    return new ReadOnlySpan<byte>(alignedBuffer.ToPointer(), length);
                }
            }
        }

        private Span<byte> WriteableSpan
        {
            get
            {
                unsafe
                {
                    return new Span<byte>(alignedBuffer.ToPointer(), length);
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                handle.Dispose();
            }
        }

        private static SampleBuffer CreateBuffer(int length)
        {
            int alignment = VectorByteSize;
            int alignedBufferSize = CalculateLengthWithAlignmentAndPadding(length);
            var buffer = Marshal.AllocHGlobal(alignedBufferSize);
            var alignedBuffer = AlignBufferStart(buffer, alignment);

            var handle = new HGlobalSafeHandle(buffer);
            var sampleBuffer = new SampleBuffer(handle, alignedBuffer, length);
            return sampleBuffer;
        }

        // Allows for alignment at buffer start and for a complete stride overrun at the
        // end of the buffer (where stride is Vector<byte>.Count).
        private static int CalculateLengthWithAlignmentAndPadding(int length) => length + (2 * VectorByteSize);

        private static IntPtr AlignBufferStart(IntPtr buffer, int alignment)
        {
            long bufAddr = buffer.ToInt64();
            long startAddr = ((bufAddr + alignment - 1) / alignment) * alignment;
            return new IntPtr(startAddr);
        }

        private static readonly int VectorByteSize = Vector<byte>.Count;

        private readonly HGlobalSafeHandle handle;
        private readonly IntPtr alignedBuffer;
        private readonly int length;

        private bool disposed;
    }
}
