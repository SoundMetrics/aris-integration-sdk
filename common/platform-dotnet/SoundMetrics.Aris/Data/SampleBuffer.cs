using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data;

/// <summary>
/// <para>
/// Implements a writeable buffer in native heap so it doesn't live
/// on the Large Object Heap (LOH). Most frames' sample size
/// dictates that it would need to live in LOH if it were
/// allocated as managed memory. Allocating on the native heap
/// instead avoids the overhead of thrashing the LOH.
/// </para>
/// <para>
/// NOTE: please do not dispose this on your own, others likely still
/// have a reference to it.
/// </para>
/// </summary>
public sealed class SampleBuffer
{
    public SampleBuffer(int length)
    {
        int alignment = VectorByteSize;
        int alignedBufferSize = CalculateLengthWithAlignmentAndPadding(length);
        var buffer = Marshal.AllocHGlobal(alignedBufferSize);

        alignedBuffer = AlignBufferStart(buffer, alignment);
        handle = new HGlobalSafeHandle(buffer);
        this.length = length;
    }

    public int Length => length;

    public Span<byte> Span
    {
        get
        {
            unsafe
            {
                return new Span<byte>(alignedBuffer.ToPointer(), length);
            }
        }
    }

    internal IntPtr UnsafeBuffer => alignedBuffer;

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

    // The allocation is owned by this SafeHandle which, on its own, will
    // be finalized when there are no more references to this instance of
    // SampleBuffer.
    //
    // This handle must be kept as it is indirectly referenced by the buffer code.
#pragma warning disable IDE0052 // Remove unread private members
    private readonly HGlobalSafeHandle handle;
#pragma warning restore IDE0052 // Remove unread private members

    private readonly IntPtr alignedBuffer;
    private readonly int length;
}
