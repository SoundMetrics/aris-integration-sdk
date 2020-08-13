namespace SoundMetrics.Aris.Device
{
    /// <summary>
    /// Describes the &quot;shape&quot; of a frame's samples.
    /// </summary>
    public struct SampleGeometry
    {
        internal SampleGeometry(
            int beamCount,
            int samplesPerBeam,
            int totalSampleCount,
            int pingsPerFrame)
        {
            BeamCount = beamCount;
            SamplesPerBeam = samplesPerBeam;
            TotalSampleCount = totalSampleCount;
            PingsPerFrame = pingsPerFrame;
        }

        public int BeamCount { get; private set; }
        public int SamplesPerBeam { get; private set; }
        public int TotalSampleCount { get; private set; }
        public int PingsPerFrame { get; private set; }

        public void Deconstruct(
            out int beamCount,
            out int samplesPerBeam,
            out int totalSampleCount,
            out int pingsPerFrame)
        {
            beamCount = BeamCount;
            samplesPerBeam = SamplesPerBeam;
            totalSampleCount = TotalSampleCount;
            pingsPerFrame = PingsPerFrame;
        }

        // Equality -----------------------------------------------------

        public override bool Equals(object? obj)
        {
            if (obj is SampleGeometry other)
            {
                return this.Equals(other);
            }

            return false;
        }

        public bool Equals(SampleGeometry other)
        {
            return
                BeamCount == other.BeamCount
                && SamplesPerBeam == other.SamplesPerBeam
                && TotalSampleCount == other.TotalSampleCount
                && PingsPerFrame == other.PingsPerFrame;
        }

        public override int GetHashCode()
        {
            return BeamCount ^ SamplesPerBeam ^ PingsPerFrame;
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
