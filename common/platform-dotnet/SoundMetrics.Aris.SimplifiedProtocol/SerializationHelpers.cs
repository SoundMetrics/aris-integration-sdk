using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    internal static class SerializationHelpers
    {
        public static T? StructFromBytes<T>(ArraySegment<byte> segment)
            where T : struct
        {
            var sizeOfT = Marshal.SizeOf<T>();

            if (segment.Count < sizeOfT)
            {
                return null;
            }

            GCHandle handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
            try
            {
                var addr = handle.AddrOfPinnedObject() + segment.Offset;
                return Marshal.PtrToStructure<T>(addr);
            }
            finally
            {
                handle.Free();
            }
        }

        internal static T? StructFromBytes<T>(byte[] bytes)
            where T : struct
        {
            return StructFromBytes<T>(new ArraySegment<byte>(bytes));
        }

        public static byte[] BytesFromStruct<T>(in T t)
            where T : struct
        {
            var sizeOfT = Marshal.SizeOf<T>();
            var buffer = new byte[sizeOfT];

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var ptr = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr<T>(t, ptr, false);
                return buffer;
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
