using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Network
{

    // Defines the metadata at the start of an ARIS frame.
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public unsafe struct FramePacketHeader
    {
        public const UInt32 FramePacketSignature = 0x53495241; // little-endian "ARIS"

        public UInt32 Signature;
        public UInt32 HeaderSize;
        public UInt32 FrameSize;
        public UInt32 FrameIndex;
        public UInt32 PartNumber;
        public UInt32 PayloadSize;
    }
}
