using Serilog;
using SoundMetrics.Aris.Headers;
using System.Collections.Generic;
using System.IO;

namespace arisfile.analysis
{
    using static FileOperations;

    internal static class ArisFrameEnumeration
    {
        public static IEnumerable<ArisFrameAccessor> EnumerateArisFrames(this Stream stream)
        {
            if (ReadArisFileHeader(stream, out var arisFileHeader))
            {
                int calculatedFrameIndex = 0;

                while (true)
                {
                    if (ReadNextFrame(
                        stream,
                        ref calculatedFrameIndex,
                        out var arisFrameAccessor))
                    {
                        yield return arisFrameAccessor;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                Log.Warning($"EnumerateArisFrames: Couldn't read file header.");
            }
        }

        private static bool ReadNextFrame(
            Stream stream,
            ref int calculatedFrameIndex,
            out ArisFrameAccessor frameAccessor)
        {
            if (ReadArisFrameHeader(stream, out var arisFrameHeader))
            {
                if (arisFrameHeader.GetBeamCount() * arisFrameHeader.SamplesPerBeam
                    is uint totalSampleCount)
                {
                    frameAccessor = new ArisFrameAccessor
                    {
                        CalculatedFrameIndex = calculatedFrameIndex,
                        ArisFrameHeader = arisFrameHeader,
                    };

                    ++calculatedFrameIndex;

                    stream.Seek(totalSampleCount, SeekOrigin.Current);

                    return true;
                }
            }

            frameAccessor = default;
            return false;
        }
    }
}
