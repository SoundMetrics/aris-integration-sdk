// Copyright 2014-2020 Sound Metrics Corp. All Rights Reserved.

using SoundMetrics.Aris.Core;
using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data
{
    internal static class FrameSampleOrder
    {
        /// <summary>
        /// Reorders the frame data from machine-order to usable order.
        /// </summary>
        /// <param name="frame">The frame to be reordered.</param>
        /// <returns>A Frame instance with reordered data.</returns>
        public static bool TryReorderFrame(Frame frame, out Frame? reorderedFrame)
        {
            if (frame.FrameHeader.ReorderedSamples != 0)
            {
                // The frame is already reordered. Other than the code that
                // initially creates the frame storage, virtually no code
                // should be aware of reordering.
                reorderedFrame = frame;
                return true;
            }

            if (SystemConfiguration.TryGetSampleGeometry(frame.FrameHeader, out var sampleGeometry))
            {
                var pingMode = (int)frame.FrameHeader.PingMode;
                var outputLength = sampleGeometry.TotalSampleCount;
                var output = Marshal.AllocHGlobal(outputLength);

                try
                {
                    IntPtr input = frame.Samples.DangerousGetHandle();
                    UnsafeReorderFrame(
                        sampleGeometry.PingsPerFrame,
                        sampleGeometry.BeamCount,
                        sampleGeometry.SampleCount,
                        input,
                        output);

#pragma warning disable CA2000 // Dispose objects before losing scope
                    // Ownership of `orderedSamples` is given away.
                    var orderedSamples = new ByteBuffer(output, outputLength);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    if (Frame.TryCreate(UpdateFrameHeader(frame.FrameHeader), orderedSamples, out reorderedFrame))
                    {
                        return true;
                    }
                    else
                    {
                        orderedSamples.Dispose();
                        return false;
                    }
                }
                catch
                {
                    Marshal.FreeHGlobal(output);
                    throw;
                }
            }
            else
            {
                reorderedFrame = default;
                return false;
            }

            static FrameHeader UpdateFrameHeader(in FrameHeader frameHeader)
            {
                var header = frameHeader;
                header.ReorderedSamples = 1;
                return header;
            }
        }

        /// <summary>
        /// Reorders samples in a frame. The signature of this function is dictated
        /// by SoundMetrics.NativeMemory.TransformFunction.
        /// C# "unsafe" reorder based on the SDK C++ code.
        /// </summary>
        /// <param name="pingsPerFrame">Number of pings per frame acquisition</param>
        /// <param name="beamCount">Number of beams</param>
        /// <param name="samplesPerBeam">Samples per beam</param>
        /// <param name="inputBuffer">The input samples</param>
        /// <param name="outputBuffer">Where to put the reordered samples (out param).</param>
        ///
        private static unsafe void UnsafeReorderFrame(
            Int32 pingsPerFrame,
            Int32 beamCount,
            Int32 samplesPerBeam,
            IntPtr inputBuffer,
            IntPtr outputBuffer)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer));
            if (outputBuffer == null)
                throw new ArgumentNullException(nameof(outputBuffer));

            const int beamsPerPing = 16;

            int[] chRvMap = { 10, 2, 14, 6, 8, 0, 12, 4, 11, 3, 15, 7, 9, 1, 13, 5 };
            int* chRvMultMap = stackalloc int[beamsPerPing];

            for (int chIdx = 0; chIdx < beamsPerPing; ++chIdx)
            {
                chRvMultMap[chIdx] = (int)(chRvMap[chIdx] * pingsPerFrame);
            }

            byte* inputByte = (byte*)inputBuffer.ToPointer();
            byte* outputBuf = (byte*)outputBuffer.ToPointer();

            for (int pingIdx = 0; pingIdx < pingsPerFrame; ++pingIdx)
            {
                for (int sampleIdx = 0; sampleIdx < samplesPerBeam; ++sampleIdx)
                {
                    int composed = sampleIdx * beamCount + pingIdx;
                    outputBuf[composed + chRvMultMap[0]] = inputByte[0];
                    outputBuf[composed + chRvMultMap[1]] = inputByte[1];
                    outputBuf[composed + chRvMultMap[2]] = inputByte[2];
                    outputBuf[composed + chRvMultMap[3]] = inputByte[3];
                    outputBuf[composed + chRvMultMap[4]] = inputByte[4];
                    outputBuf[composed + chRvMultMap[5]] = inputByte[5];
                    outputBuf[composed + chRvMultMap[6]] = inputByte[6];
                    outputBuf[composed + chRvMultMap[7]] = inputByte[7];
                    outputBuf[composed + chRvMultMap[8]] = inputByte[8];
                    outputBuf[composed + chRvMultMap[9]] = inputByte[9];
                    outputBuf[composed + chRvMultMap[10]] = inputByte[10];
                    outputBuf[composed + chRvMultMap[11]] = inputByte[11];
                    outputBuf[composed + chRvMultMap[12]] = inputByte[12];
                    outputBuf[composed + chRvMultMap[13]] = inputByte[13];
                    outputBuf[composed + chRvMultMap[14]] = inputByte[14];
                    outputBuf[composed + chRvMultMap[15]] = inputByte[15];
                    inputByte += beamsPerPing;
                }
            }
        }
    }
}
