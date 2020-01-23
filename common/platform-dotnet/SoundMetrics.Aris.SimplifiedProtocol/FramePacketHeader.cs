using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    using static SerializationHelpers;

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct FramePacketHeader
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

    public static class FramePacketHeaderExtensions
    {
        public static FramePacketHeader? FromBytes(byte[] bytes)
        {
            var headerSize = Marshal.SizeOf<FramePacketHeader>();
            Debug.Assert(headerSize == 20);

            var newHeader = StructFromBytes<FramePacketHeader>(bytes);
            if (newHeader.HasValue)
            {
                if (Validate(newHeader.Value))
                {
                    return newHeader;
                }
            }

            return null;

            bool Validate(in FramePacketHeader headerToValidate)
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
