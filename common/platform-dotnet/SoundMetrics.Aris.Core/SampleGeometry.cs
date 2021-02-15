// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    /// <summary>
    /// Describes the &quot;shape&quot; of a frame's samples.
    /// </summary>
    public struct SampleGeometry : IEquatable<SampleGeometry>
    {
        internal SampleGeometry(
            int beamCount,
            int sampleCount,
            int totalSampleCount,
            int pingsPerFrame)
        {
            BeamCount = beamCount;
            SampleCount = sampleCount;
            TotalSampleCount = totalSampleCount;
            PingsPerFrame = pingsPerFrame;
        }

        public int BeamCount { get; private set; }
        public int SampleCount { get; private set; }
        public int TotalSampleCount { get; private set; }
        public int PingsPerFrame { get; private set; }

        public override string ToString() =>
            $"BeamCount={BeamCount}; SampleCount={SampleCount:N0}; "
                + $"TotalSampleCount={TotalSampleCount:N0}; PingsPerFrame={PingsPerFrame}";

        public void Deconstruct(
            out int beamCount,
            out int sampleCount,
            out int totalSampleCount,
            out int pingsPerFrame)
        {
            beamCount = BeamCount;
            sampleCount = SampleCount;
            totalSampleCount = TotalSampleCount;
            pingsPerFrame = PingsPerFrame;
        }

        // Equality -----------------------------------------------------

        public override bool Equals(object obj)
            => obj is SampleGeometry other && this.Equals(other);

        public bool Equals(SampleGeometry other)
            => BeamCount == other.BeamCount
                && SampleCount == other.SampleCount
                && TotalSampleCount == other.TotalSampleCount
                && PingsPerFrame == other.PingsPerFrame;

        public override int GetHashCode()
        {
            return BeamCount ^ SampleCount ^ PingsPerFrame;
        }

        public static bool operator ==(SampleGeometry lhs, SampleGeometry rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(SampleGeometry lhs, SampleGeometry rhs)
        {
            return !(lhs.Equals(rhs));
        }
    }
}
