// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

using System;

namespace SoundMetrics.Aris.ReorderCS
{
    /// <summary>
    /// C# "unsafe" reorder based on the SDK C++ code.
    /// </summary>
    public static class Reorder
    {
        /// <summary>
        /// Reorders samples in a frame. The signature of this function is dictated
        /// by SoundMetrics.NativeMemory.TransformFunction.
        /// </summary>
        /// <param name="pingMode">ARIS ping mode</param>
        /// <param name="pingsPerFrame">Number of pings per frame acquisition</param>
        /// <param name="beamCount">Number of beams</param>
        /// <param name="samplesPerBeam">Samples per beam</param>
        /// <param name="inputBuffer">The input samples</param>
        /// <param name="outputBuffer">Where to put the reordered samples (out param).</param>
        public static unsafe void ReorderFrame(
            UInt32 pingMode,
            UInt32 pingsPerFrame,
            UInt32 beamCount,
            UInt32 samplesPerBeam,
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

            for (uint pingIdx = 0; pingIdx < pingsPerFrame; ++pingIdx)
            {
                for (uint sampleIdx = 0; sampleIdx < samplesPerBeam; ++sampleIdx)
                {
                    uint composed = sampleIdx * beamCount + pingIdx;
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

        private static uint PingModeToPingsPerFrame(uint pingMode)
        {
            switch (pingMode)
            {
                case 1: return 3;
                case 3: return 6;
                case 6: return 4;
                case 9: return 8;
                default: return 0;
            }
        }

        private static uint PingModeToNumBeams(uint pingMode)
        {
            switch (pingMode)
            {

                case 1: return 48;
                case 3: return 96;
                case 6: return 64;
                case 9: return 128;
                default: return 0;
            }
        }
    }
}
