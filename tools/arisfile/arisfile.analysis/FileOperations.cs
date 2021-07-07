using SoundMetrics.Aris.Headers;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace arisfile.analysis
{
    internal static class FileOperations
    {
        public static bool ReadArisFileHeader(Stream stream, out ArisFileHeader arisFileHeader)
        {
            return ReadStructFromStream(stream, out arisFileHeader);
        }

        public static bool ReadArisFrameHeader(Stream stream, out ArisFrameHeader arisFrameHeader)
        {
            return ReadStructFromStream(stream, out arisFrameHeader);
        }

        private static bool ReadStructFromStream<T>(Stream stream, out T t) where T : struct
        {
            var expectedLength = Marshal.SizeOf<T>();
            var buffer = new byte[expectedLength];

            var bytesRead = stream.Read(buffer);
            if (bytesRead != buffer.Length)
            {
                if (bytesRead == 0)
                {
                    t = default(T);
                    return false;
                }

                throw new Exception("Couldn't read an entire file header");
            }

            var h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                t = Marshal.PtrToStructure<T>(h.AddrOfPinnedObject());
                return true;
            }
            finally
            {
                h.Free();
            }
        }
    }
}
