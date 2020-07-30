using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.Data
{
    internal static class Serialization
    {
        public static unsafe bool ReadStruct<T>(this Stream stream, out T t)
            where T : unmanaged
        {
            fixed (T* pt = &t)
            {
                var span = new Span<T>(pt, 1);
                var byteSpan = MemoryMarshal.AsBytes(span);
                var bytesRead = stream.Read(byteSpan);
                return bytesRead == byteSpan.Length;
            }
        }

        public static unsafe void WriteStruct<T>(this Stream stream, in T t)
            where T : unmanaged
        {
            fixed (T* pt = &t)
            {
                var span = new Span<T>(pt, 1);
                var byteSpan = MemoryMarshal.AsBytes(span);
                stream.Write(byteSpan);
            }
        }
    }
}
