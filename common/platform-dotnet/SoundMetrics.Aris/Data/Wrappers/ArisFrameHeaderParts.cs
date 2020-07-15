using System;

namespace SoundMetrics.Aris.Data.Wrappers
{
    public ref struct ArisFrameHeaderParts
    {
        public ArisFrameHeaderParts(Span<ArisFrameHeader> frameHeader)
        {
            this.frameHeader = frameHeader;
        }

        public bool HasValidSignature { get => frameHeader[0].Version == ArisFrameHeader.ArisFrameSignature; }

        public ArisFrameHeaderIdentity Identity { get => new ArisFrameHeaderIdentity(this); }
        public ArisFrameHeaderTime Time { get => new ArisFrameHeaderTime(this); }
        public ArisFrameHeaderEnvironment Environment{ get => new ArisFrameHeaderEnvironment(this); }

        internal Span<ArisFrameHeader> FrameHeader { get => frameHeader; }

        private readonly Span<ArisFrameHeader> frameHeader;
    }

    //--------------------------------------------------------------------------

    public static class ArisFrameHeaderPartsExtensions
    {
        public delegate T WrapperAction<T>(in ArisFrameHeaderParts parts);

        public static unsafe T WithParts<T>(
            this in ArisFrameHeader frameHeader,
            WrapperAction<T> withParts)
        {
            fixed (ArisFrameHeader* pfh = &frameHeader)
            {
                var hdr = new Span<ArisFrameHeader>(pfh, 1);
                return withParts(new ArisFrameHeaderParts(hdr));
            }
        }
    }

    //--------------------------------------------------------------------------

    public ref struct ArisFrameHeaderIdentity
    {
        public ArisFrameHeaderIdentity(ArisFrameHeaderParts parts)
        {
            this.parts = parts;
        }

        public SystemType SystemType { get => (SystemType)parts.FrameHeader[0].TheSystemType; }

        /// Sonar serial number as labeled on housing.
        public uint SerialNumber { get => parts.FrameHeader[0].SonarSerialNumber; }

        public uint FrameIndex { get => parts.FrameHeader[0].FrameIndex; }

        private readonly ArisFrameHeaderParts parts;
    }

    //--------------------------------------------------------------------------

    public ref struct ArisFrameHeaderTime
    {
        public ArisFrameHeaderTime(ArisFrameHeaderParts parts)
        {
            this.parts = parts;
        }

        public DateTime SonarTimestamp
        {
            get
            {
                return ArisFrameHeaderExtensions.SonarOffsetToDateTime(parts.FrameHeader[0].sonarTimeStamp);
            }
        }

        public ulong GoTime { get => parts.FrameHeader[0].GoTime; }
        public DateTime GoTimestamp
        {
            get
            {
                return ArisFrameHeaderExtensions.SonarOffsetToDateTime(GoTime);
            }
        }

        public ulong TopsideTime { get => parts.FrameHeader[0].FrameTime; }
        public DateTime TopsideTimestamp
        {
            get
            {
                return ArisFrameHeaderExtensions.SonarOffsetToDateTime(TopsideTime);
            }
        }

        /// Instantaneous frame rate between frame N and frame N-1
        public float InstantaneousFrameRate
        {
            get => parts.FrameHeader[0].FrameRate;
        }


        public TimeSpan? GpsTimeAge
        {
            get
            {
                var age = parts.FrameHeader[0].GpsTimeAge;
                return age == 0 ? null : (TimeSpan?)TimeSpan.FromMilliseconds(age / 1000.0);
            }
        }

        public TimeSpan Uptime { get => TimeSpan.FromSeconds(parts.FrameHeader[0].Uptime); }

        private readonly ArisFrameHeaderParts parts;
    }

    //--------------------------------------------------------------------------

    public ref struct ArisFrameHeaderEnvironment
    {
        public ArisFrameHeaderEnvironment(ArisFrameHeaderParts parts)
        {
            this.parts = parts;
        }

        /// Water salinity code:  0 = fresh, 15 = brackish, 35 = salt
        public uint Salinity { get => parts.FrameHeader[0].Salinity; }

        /// Depth sensor output. Note: psi
        public float Pressure { get => parts.FrameHeader[0].Pressure; }

        public float WaterTemp { get => parts.FrameHeader[0].WaterTemp; }

        /// Sound velocity in water calculated from water temperature depth and salinity setting.
        /// Note: m/s
        public float SoundSpeed { get => parts.FrameHeader[0].SoundSpeed; }

        private readonly ArisFrameHeaderParts parts;
    }
}
