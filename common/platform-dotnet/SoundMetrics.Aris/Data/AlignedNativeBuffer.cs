using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data;

public sealed class AlignedNativeBuffer
{
    public AlignedNativeBuffer(
        int alignment,
        int length)
    {
        Alignment = alignment;
        Length = length;

        int alignedBufferSize = CalculateLengthWithAlignmentAndPadding(length, alignment);
        var buffer = Marshal.AllocHGlobal(alignedBufferSize);

        alignedBuffer = AlignBufferStart(buffer, alignment);
        handle = new HGlobalSafeHandle(buffer);
    }

    public int Length { get; }

    public int Alignment { get; }

    public IntPtr UnsafeBuffer => alignedBuffer;

    public unsafe Span<byte> Span => new Span<byte>((void*)alignedBuffer, Length);

    private readonly IntPtr alignedBuffer;

    // The allocation is owned by this SafeHandle which, on its own, will
    // be finalized when there are no more references to this instance of
    // SampleBuffer.
    //
    // This handle must be kept as it is indirectly referenced by the buffer code.
    private readonly HGlobalSafeHandle handle;

    private static IntPtr AlignBufferStart(IntPtr buffer, int alignment)
    {
        long bufAddr = buffer.ToInt64();
        long startAddr = ((bufAddr + alignment - 1) / alignment) * alignment;
        return new IntPtr(startAddr);
    }

    // Allows for alignment at buffer start and for a complete stride overrun at the
    // end of the buffer (where stride is Vector<byte>.Count).
    private static int CalculateLengthWithAlignmentAndPadding(int length, int alignment) => length + (2 * alignment);
}
