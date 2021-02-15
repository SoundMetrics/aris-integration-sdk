using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Network
{

    // Defines the metadata at the start of an ARIS frame.
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public unsafe struct FramePacketHeader
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public const UInt32 FramePacketSignature = 0x53495241; // little-endian "ARIS"

#pragma warning disable CA1051 // Do not declare visible instance fields
        public UInt32 Signature;
        public UInt32 HeaderSize;
        public UInt32 FrameSize;
        public UInt32 FrameIndex;
        public UInt32 PartNumber;
        public UInt32 PayloadSize;
#pragma warning restore CA1051 // Do not declare visible instance fields
    }
}
