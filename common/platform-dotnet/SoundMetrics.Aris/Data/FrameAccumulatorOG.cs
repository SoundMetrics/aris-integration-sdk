using Serilog;
using SoundMetrics.Aris.Core;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data;

internal sealed class FrameAccumulatorOG
{
    public FrameAccumulatorOG(
        int frameIndex,
        byte[] header,
        DateTimeOffset timestamp,
        ReadOnlySpan<byte> firstDataFragment,
        int totalDataSize)
    {
        sampleData = new(totalDataSize);
        fi = frameIndex >= 0 ? frameIndex : throw new ArgumentOutOfRangeException(nameof(frameIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(header.Length, 0);

        FrameReceiptTimestamp = timestamp;
        if (!TryGetFrameHeaderFromBuffer(header, out frameHeader))
        {
            throw new Exception("Invalid frame header");
        }
    }

    public void AppendFrameData(int dataOffset, ReadOnlySpan<byte> dataFragment)
    {
        Debug.Assert(dataOffset + dataFragment.Length <= sampleData.Length);

        // The sliding window code elsewhere deals with whether we succeed or
        // fail on missing data; here we just store it away.
        var destination = sampleData.Span.Slice(dataOffset, dataFragment.Length);
        dataFragment.CopyTo(destination);

        BytesReceived += dataFragment.Length;
    }

    /// <summary>
    /// Allows us to manipulate the frame index along the way.
    /// </summary>
    /// <param name="newFrameIndex">The desired frame index.</param>
    public void SetFrameIndex(int newFrameIndex) => fi = newFrameIndex;

    public int FrameIndex => fi;

    public bool IsComplete => BytesReceived == sampleData.Length;

    public int BytesReceived { get; private set; } = 0;

    public int ExpectedSize => sampleData.Length;

    public DateTimeOffset FrameReceiptTimestamp { get; }

    public bool MakeCompletedFrame(
        [NotNullWhen(true)] out Frame? completedFrame)
    {
        if (IsComplete)
        {
            completedFrame =
                Frame.Create(frameHeader, ReadOnlySampleBuffer.CopyFrom(sampleData.Span));
            return true;
        }
        else
        {
            completedFrame = null;
            return false;
        }
    }

    private static bool TryGetFrameHeaderFromBuffer(
        ReadOnlyMemory<byte> frameHeaderBytes,
        out FrameHeader frameHeader)
    {
        // The size of the frame header bytes as transmitted is smaller than the
        // size of FrameHeader.
        int fullFrameHeaderLength = Marshal.SizeOf<FrameHeader>();

        unsafe
        {
            fixed (FrameHeader* pHeader = &frameHeader)
            {
                var outputSpan = new Span<byte>(pHeader, fullFrameHeaderLength);
                frameHeaderBytes.Span.CopyTo(outputSpan);
            }
        }

        return true;
    }

    private readonly FrameHeader frameHeader;
    private readonly SampleBuffer sampleData; // SampleBuffer keeps us out of the LOH, uses unmanaged memory.

    private int fi;
}
