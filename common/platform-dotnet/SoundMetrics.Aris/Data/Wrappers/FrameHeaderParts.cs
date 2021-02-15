using SoundMetrics.Aris.Core;
using System;

namespace SoundMetrics.Aris.Data.Wrappers
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public ref struct FrameHeaderParts
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public FrameHeaderParts(Span<FrameHeader> frameHeader)
        {
            this.frameHeader = frameHeader;
        }

        public bool HasValidSignature { get => frameHeader[0].Version == Core.FrameHeader.ArisFrameSignature; }

        public ArisFrameHeaderIdentity Identity { get => new ArisFrameHeaderIdentity(this); }
        public ArisFrameHeaderTime Time { get => new ArisFrameHeaderTime(this); }
        public ArisFrameHeaderEnvironment Environment{ get => new ArisFrameHeaderEnvironment(this); }

        internal Span<FrameHeader> FrameHeader { get => frameHeader; }

        private readonly Span<FrameHeader> frameHeader;
    }

    //--------------------------------------------------------------------------

    public static class ArisFrameHeaderPartsExtensions
    {
        public delegate T WrapperAction<T>(in FrameHeaderParts parts);

        public static unsafe T WithParts<T>(
            this in FrameHeader frameHeader,
            WrapperAction<T> withParts)
        {
            if (withParts is null) throw new ArgumentNullException(nameof(withParts));

            fixed (FrameHeader* pfh = &frameHeader)
            {
                var hdr = new Span<FrameHeader>(pfh, 1);
                return withParts(new FrameHeaderParts(hdr));
            }
        }
    }

    //--------------------------------------------------------------------------

#pragma warning disable CA1815 // Override equals and operator equals on value types
    public ref struct ArisFrameHeaderIdentity
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public ArisFrameHeaderIdentity(FrameHeaderParts parts)
        {
            this.parts = parts;
            this.SystemType =
                SystemType.GetFromIntegralValue((int)parts.FrameHeader[0].TheSystemType);
        }

        public SystemType SystemType { get; private set; }

        /// Sonar serial number as labeled on housing.
        public uint SerialNumber { get => parts.FrameHeader[0].SonarSerialNumber; }

        public uint FrameIndex { get => parts.FrameHeader[0].FrameIndex; }

        private readonly FrameHeaderParts parts;
    }

    //--------------------------------------------------------------------------

#pragma warning disable CA1815 // Override equals and operator equals on value types
    public ref struct ArisFrameHeaderTime
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public ArisFrameHeaderTime(FrameHeaderParts parts)
        {
            this.parts = parts;
        }

        public DateTime SonarTimestamp
        {
            get
            {
                return FrameHeaderExtensions.SonarOffsetToDateTime(parts.FrameHeader[0].sonarTimeStamp);
            }
        }

        public ulong GoTime { get => parts.FrameHeader[0].GoTime; }
        public DateTime GoTimestamp
        {
            get
            {
                return FrameHeaderExtensions.SonarOffsetToDateTime(GoTime);
            }
        }

        public ulong TopsideTime { get => parts.FrameHeader[0].FrameTime; }
        public DateTime TopsideTimestamp
        {
            get
            {
                return FrameHeaderExtensions.SonarOffsetToDateTime(TopsideTime);
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

        private readonly FrameHeaderParts parts;
    }

    //--------------------------------------------------------------------------

#pragma warning disable CA1815 // Override equals and operator equals on value types
    public ref struct ArisFrameHeaderEnvironment
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public ArisFrameHeaderEnvironment(FrameHeaderParts parts)
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

        private readonly FrameHeaderParts parts;
    }
}
