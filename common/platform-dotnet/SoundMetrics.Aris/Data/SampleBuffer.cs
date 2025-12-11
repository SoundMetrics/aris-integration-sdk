using System;
using System.Numerics;

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
        : this(new AlignedNativeBuffer(VectorByteSize, length))
    {
    }

    public SampleBuffer(AlignedNativeBuffer alignedNativeBuffer)
    {
        int expectedAlignment = VectorByteSize;

        if (alignedNativeBuffer.Alignment != expectedAlignment)
        {
            throw new Exception(
                $"Alignment value [{alignedNativeBuffer.Alignment}] of native buffer "
                + $"is not the expected alignment of [{expectedAlignment}]");
        }

        this.alignedNativeBuffer = alignedNativeBuffer;
    }

    public int Length => alignedNativeBuffer.Length;

    public static int Alignment => VectorByteSize;

    public Span<byte> Span => alignedNativeBuffer.Span;

    internal IntPtr UnsafeBuffer => alignedNativeBuffer.UnsafeBuffer;

    private static readonly int VectorByteSize = Vector<byte>.Count;

    private readonly AlignedNativeBuffer alignedNativeBuffer;
}
