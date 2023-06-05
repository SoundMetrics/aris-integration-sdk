using Serilog;
using SoundMetrics.Aris.Headers;
using System;
using System.IO;

namespace arisfile.analysis
{
    using static FileOperations;

    public static class Analysis
    {
        public delegate void FrameAnalysisFunction(ArisFrameAccessor frameAccessor);

        public static void Run(
            Stream stream,
            params FrameAnalysisFunction[] analysisFunctions)
        {
            CheckFileTraits(stream);
            EnumerateFramesForAnalysis(stream, analysisFunctions);
        }

        private static void EnumerateFramesForAnalysis(
            Stream stream,
            FrameAnalysisFunction[] analysisFunctions)
        {
            stream.Seek(0, SeekOrigin.Begin);
            foreach (var frame in new ArisFile(stream).Frames)
            {
                foreach (var afn in analysisFunctions)
                {
                    afn(frame);
                }
            }
        }

        private static void CheckFileTraits(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            ArisFileHeader arisFileHeader;
            ReadArisFileHeader(stream, out arisFileHeader);
            var hasExpectedSignature = arisFileHeader.Version == ArisFileHeader.ArisFileSignature;

            if (!hasExpectedSignature)
            {
                Log.Warning("File does not have the expected file signature");
            }

            //CheckFrameHeaders(stream);
        }

        //private static void CheckFrameHeaders(Stream stream)
        //{
        //    using (var _ = new PositionAfterScope(stream))
        //    {
        //        var (firstHeader, firstTotalSamples) = GetFrameHeaderInfo(stream);
        //        ArisFrameHeader firstArisFrameHeader;
        //        if (ReadArisFrameHeader(stream, out firstArisFrameHeader))
        //        {
        //            if (firstArisFrameHeader.GetBeamCount() is uint beamCount)
        //            {
        //                var totalSampleCount =
        //                    firstArisFrameHeader.SamplesPerBeam * beamCount;
        //                Log.Information(
        //                    "First frame: total sample count is {totalSampleCount} ({beamCount} \u00d7 {sampleCount})",
        //                    totalSampleCount,
        //                    beamCount,
        //                    firstArisFrameHeader.SamplesPerBeam);
        //            }
        //            else
        //            {
        //                throw new ArgumentOutOfRangeException("Couldn't get the frame's beam count");
        //            }
        //        }
        //    }

        //    (ArisFrameHeader header, uint totalSampleCount)
        //        GetFrameHeaderInfo(Stream stream)
        //    {
        //        ArisFrameHeader header;
        //        if (ReadArisFrameHeader(stream, out header))
        //        {
        //            if (header.GetBeamCount() is uint beamCount)
        //            {
        //                var totalSampleCount =
        //                    header.SamplesPerBeam * beamCount;
        //                Log.Information(
        //                    "First frame: total sample count is {totalSampleCount} ({beamCount} \u00d7 {sampleCount})",
        //                    totalSampleCount,
        //                    beamCount,
        //                    header.SamplesPerBeam);

        //                return (header, totalSampleCount);
        //            }
        //            else
        //            {
        //                throw new ArgumentOutOfRangeException("Couldn't get the frame's beam count");
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("Couldn't read a whole frame header");
        //        }
        //    }
        //}

        private class PositionAfterScope : IDisposable
        {
            public PositionAfterScope(Stream stream)
            {
                this.stream = stream;
                position = stream.Position;
            }

            public void Dispose()
            {
                stream.Seek(position, SeekOrigin.Begin);
            }

            private readonly Stream stream;
            private readonly long position;
        }
    }
}
