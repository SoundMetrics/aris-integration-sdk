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
    public sealed class SampleBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        public delegate void InitializeBufferSpan(Span<byte> buffer);
        public unsafe delegate void InitializeBufferUnsafe(byte* buffer, int length);

        public delegate void TransformBufferFn(
            ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);

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

            var buffer = Marshal.AllocHGlobal(length);

            try
            {
                unsafe
                {
                    var writeableBuffer = new Span<byte>(buffer.ToPointer(), length);
                    initializeBuffer(writeableBuffer);
                }
            }
            catch
            {
                Marshal.FreeHGlobal(buffer);
                throw;
            }

            return new SampleBuffer(buffer, length);
        }

        public unsafe static SampleBuffer Create(int length, InitializeBufferUnsafe initializeBuffer)
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

            var buffer = Marshal.AllocHGlobal(length);

            try
            {
                unsafe
                {
                    var ptr = buffer.ToPointer();
                    initializeBuffer((byte*)ptr, length);
                }
            }
            catch
            {
                Marshal.FreeHGlobal(buffer);
                throw;
            }

            return new SampleBuffer(buffer, length);
        }

        public unsafe static SampleBuffer Create(ReadOnlySpan<byte> source)
        {
            var length = source.Length;
            var buffer = Marshal.AllocHGlobal(length);

            try
            {
                unsafe
                {
                    var destination = new Span<byte>(buffer.ToPointer(), length);
                    source.CopyTo(destination);
                    return new SampleBuffer(buffer, length);
                }
            }
            catch
            {
                Marshal.FreeHGlobal(buffer);
                throw;
            }
        }

        /// <summary>
        /// Construct from a list of buffers.
        /// </summary>
        public unsafe static SampleBuffer Create(IEnumerable<ReadOnlyMemory<byte>> sourceBuffers)
        {
            var sources = sourceBuffers.ToArray();
            var totalLength = sources.Sum(source => source.Length);
            var buffer = Marshal.AllocHGlobal(totalLength);

            try
            {
                var fullDestination = new Span<byte>(buffer.ToPointer(), totalLength);
                int offset = 0;

                foreach (var source in sources)
                {
                    var partialOutput = fullDestination.Slice(offset, source.Length);
                    source.Span.CopyTo(partialOutput);
                    offset += partialOutput.Length;
                }

                return new SampleBuffer(buffer, totalLength);
            }
            catch
            {
                Marshal.FreeHGlobal(buffer);
                throw;
            }
        }

        /// <summary>
        /// Build from an existing buffer. Generally not prefered.
        /// </summary>
        internal SampleBuffer(IntPtr buffer, int bufferLength)
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

        public SampleBuffer Transform(TransformBufferFn transformBuffer)
        {
            void initialize(Span<byte> output) => transformBuffer(this.Span, output);

            var newBuffer = SampleBuffer.Create(length, initialize);
            return newBuffer;
        }

        private readonly int length;
    }
}
