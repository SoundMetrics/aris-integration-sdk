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
        public uint Signature;

        /// <summary>
        /// The size of this header.
        /// </summary>
        public uint HeaderSize;

        /// <summary>
        /// The count of the frame's sample bytes.
        /// </summary>
        public uint FrameSize;

        /// <summary>
        /// This frame's index.
        /// </summary>
        public uint FrameIndex;

        /// <summary>
        /// Zero-based index of this part for this frame.
        /// </summary>
        public uint PartNumber;
    }

    public static class FramePartHeaderExtensions
    {
        public static bool FromBytes(byte[] bytes, out FramePartHeader header)
        {
            var headerSize = Marshal.SizeOf<FramePartHeader>();
            Debug.Assert(headerSize == 20);

            if (bytes.Length < headerSize)
            {
                header = new FramePartHeader();
                return false;
            }

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                FramePartHeader newHeader = (FramePartHeader)
                    Marshal.PtrToStructure<FramePartHeader>(handle.AddrOfPinnedObject());

                if (Validate(newHeader))
                {
                    header = newHeader;
                    return true;
                }
                else
                {
                    header = new FramePartHeader();
                    return false;
                }
            }
            finally
            {
                handle.Free();
            }

            bool Validate(in FramePartHeader headerToValidate)
            {
                var success =
                    headerToValidate.Signature == 0x53495241 // "ARIS"
                    && headerToValidate.HeaderSize >= headerSize
                    ;
                return success;
            }
        }
    }
}
