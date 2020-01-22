using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct FramePartHeader
    {
        /// <summary>
        /// Should be 0x53495241.
        /// </summary>
        public UInt32 Signature;

        /// <summary>
        /// The size of this header.
        /// </summary>
        public UInt32 HeaderSize;

        /// <summary>
        /// The count of the frame's sample bytes.
        /// </summary>
        public UInt32 FrameSize;

        /// <summary>
        /// This frame's index.
        /// </summary>
        public UInt32 FrameIndex;

        /// <summary>
        /// Zero-based index of this part for this frame.
        /// </summary>
        public UInt32 PartNumber;

        public bool FromBytes(ArraySegment<byte> bytes, out FramePartHeader header)
        {
            var headerSize = Marshal.SizeOf<FramePartHeader>();
            Debug.Assert(headerSize == 20);

            if (bytes.Count < headerSize)
            {
                header = new FramePartHeader();
                return false;
            }

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                header =
                    (FramePartHeader)
                    Marshal.PtrToStructure<FramePartHeader>(handle.AddrOfPinnedObject());
                return true;
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
