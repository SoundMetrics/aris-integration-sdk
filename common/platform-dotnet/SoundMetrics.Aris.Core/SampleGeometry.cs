// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core
{
    public record class SampleGeometry(
        int BeamCount,
        int SampleCount,
        int TotalSampleCount,
        int PingsPerFrame)
    {
        public static readonly SampleGeometry Invalid = new(0, 0, 0, 0);
    }
}
