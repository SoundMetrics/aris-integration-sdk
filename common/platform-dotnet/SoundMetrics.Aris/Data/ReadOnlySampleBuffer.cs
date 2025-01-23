using SoundMetrics.Aris.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data;

/// <summary>
/// Implements a read-only buffer in native heap so it doesn't live
/// on the Large Object Heap (LOH). Most frames' sample size
/// dictates that it would need to live in LOH if it were
/// allocated as managed memory. Allocating on the native heap
/// instead avoids the overhead of thrashing the LOH.
/// </summary>
public sealed class ReadOnlySampleBuffer
{
    public delegate void InitializeBufferSpan(
        SampleGeometry sampleGeometry,
        Span<byte> buffer);
    public delegate void InitializeBuffer(
        SampleGeometry sampleGeometry,
        IntPtr buffer,
        int length);

    public delegate void TransformBufferSpan(
        SampleGeometry sampleGeometry,
        ReadOnlySpan<byte> inputBuffer,
        Span<byte> outputBuffer);
    public delegate void TransformBuffer(
        SampleGeometry sampleGeometry,
        IntPtr inputBuffer,
        IntPtr outputBuffer,
        int length);

    private ReadOnlySampleBuffer(SampleBuffer sampleBuffer)
    {
        buffer = sampleBuffer;
    }

    public static ReadOnlySampleBuffer Create(
        SampleGeometry sampleGeometry,
        int length,
        InitializeBufferSpan initializeBuffer)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                $"{nameof(length)} must be greater than zero");
        }

        if (initializeBuffer is null)
        {
            throw new ArgumentNullException(nameof(initializeBuffer));
        }

        var writeableBuffer = new SampleBuffer(length);
        initializeBuffer(sampleGeometry, writeableBuffer.Span);

        return new ReadOnlySampleBuffer(writeableBuffer);
    }

    public static ReadOnlySampleBuffer Create(
        SampleGeometry sampleGeometry,
        int length, 
        InitializeBuffer initializeBuffer)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                $"{nameof(length)} must be greater than zero");
        }

        if (initializeBuffer is null)
        {
            throw new ArgumentNullException(nameof(initializeBuffer));
        }

        var writeableBuffer = new SampleBuffer(length);
        initializeBuffer(sampleGeometry, writeableBuffer.UnsafeBuffer, length);

        return new ReadOnlySampleBuffer(writeableBuffer);
    }

    public static ReadOnlySampleBuffer Create(ReadOnlySpan<byte> source)
    {
        var writeableBuffer = new SampleBuffer(source.Length);
        source.CopyTo(writeableBuffer.Span);

        return new ReadOnlySampleBuffer(writeableBuffer);
    }

    /// <summary>
    /// Construct from a list of buffers.
    /// </summary>
    public unsafe static ReadOnlySampleBuffer Create(IEnumerable<ReadOnlyMemory<byte>> sourceBuffers)
    {
        var sources = sourceBuffers.ToArray();
        var totalLength = sources.Sum(source => source.Length);

        var writeableBuffer = new SampleBuffer(totalLength);

        var fullDestination = writeableBuffer.Span;
        int offset = 0;

        foreach (var source in sources)
        {
            var partialOutput = fullDestination.Slice(offset, source.Length);
            source.Span.CopyTo(partialOutput);
            offset += partialOutput.Length;
        }

        return new ReadOnlySampleBuffer(writeableBuffer);
    }

    public ReadOnlySampleBuffer Transform(
        SampleGeometry sampleGeometry,
        TransformBufferSpan transformBuffer)
    {
        void initialize(SampleGeometry sampleGeometry, Span<byte> output) 
            => transformBuffer(sampleGeometry, this.Span, output);

        var newBuffer = ReadOnlySampleBuffer.Create(sampleGeometry, buffer.Length, initialize);
        return newBuffer;
    }

    public unsafe ReadOnlySampleBuffer Transform(
        SampleGeometry sampleGeometry,
        TransformBuffer transformBufferUnsafe)
    {
        void initialize(SampleGeometry sampleGeometry, IntPtr outputBuffer, int length)
            => transformBufferUnsafe(sampleGeometry, buffer.UnsafeBuffer, outputBuffer, length);

        var newBuffer = ReadOnlySampleBuffer.Create(sampleGeometry, buffer.Length, initialize);
        return newBuffer;
    }

    public int Length => buffer.Length;

    public ReadOnlySpan<byte> Span => buffer.Span;

    private readonly SampleBuffer buffer;
}
