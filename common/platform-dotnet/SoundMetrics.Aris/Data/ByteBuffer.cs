using Microsoft.Win32.SafeHandles;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data
{
    /// <summary>
    /// Implements a buffer in native heap so it doesn't live
    /// on the Large Object Heap (LOH).
    /// </summary>
    public sealed class ByteBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        public delegate void InitializeBuffer(Span<byte> buffer);
        public delegate void TransformBuffer(
            ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);

        public ByteBuffer(int length, InitializeBuffer initializeBuffer)
            : base(ownsHandle: true)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    $"{nameof(length)} must be greater than zero");
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

        public ByteBuffer(Memory<byte>[] buffers)
            : this(SumBufferLengths(buffers), CreateInitializer(buffers))
        {

        }

        private static int SumBufferLengths(Memory<byte>[] buffers) =>
            buffers.Sum(buffer => buffer.Length);

        private static InitializeBuffer CreateInitializer(Memory<byte>[] buffers)
        {
            return output =>
            {
                int offset = 0;
                foreach (var buffer in buffers)
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

        public ByteBuffer Transform(TransformBuffer transformBuffer)
        {
            void initialize(Span<byte> output) => transformBuffer(this.Span, output);

            var newBuffer = new ByteBuffer(length, initialize);
            return newBuffer;
        }

        private readonly int length;
    }
}
