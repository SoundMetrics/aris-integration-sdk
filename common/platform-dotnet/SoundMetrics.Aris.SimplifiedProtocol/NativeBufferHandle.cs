using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    public sealed class NativeBufferHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private NativeBufferHandle(IntPtr hBuffer, int bufferLength)
            : base(ownsHandle: true)
        {
            base.SetHandle(hBuffer);
            this.bufferLength = bufferLength;
        }

        public NativeBufferHandle(int bufferLength)
            : this(Marshal.AllocHGlobal(bufferLength), bufferLength)
        {
        }

        public NativeBufferHandle(ArraySegment<byte> contents)
            : this(contents.Count)
        {
            Initialize(contents);
        }

        public NativeBufferHandle(IEnumerable<ArraySegment<byte>> contents)
            : this(CacheToArray(contents))
        {
        }

        private NativeBufferHandle(ArraySegment<byte>[] contents)
            : this(TotalSize(contents))
        {
            Initialize(contents);
        }

        private static int TotalSize(ArraySegment<byte>[] cs)
        {
            return cs.Sum(c => c.Count);
        }

        private static ArraySegment<byte>[] CacheToArray(
            IEnumerable<ArraySegment<byte>> contents
        )
            => (contents is ArraySegment<byte>[] array) ? array : contents.ToArray();

        private void Initialize(ArraySegment<byte> contents)
        {
            InitializeSegment(contents, 0);
        }

        private void Initialize(IEnumerable<ArraySegment<byte>> contents)
        {
            var segments =
                (contents is ArraySegment<byte>[] array) ? array : contents.ToArray();

            int offset = 0;

            foreach (var segment in segments)
            {
                InitializeSegment(segment, offset);
                offset += segment.Count;
            }
        }

        private void InitializeSegment(ArraySegment<byte> contents, int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    $"offset is {offset}"
                    );
            }

            if (offset >= Length || offset + contents.Count > Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contents),
                    "The contents do not fit in this buffer"
                    );
            }

            var source = contents.Array;
            var startIndex = contents.Offset;
            var ptr = Handle + offset;
            Marshal.Copy(source, startIndex, ptr, contents.Count);
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(Handle);
            return true;
        }

        internal IntPtr Handle { get => base.DangerousGetHandle(); }

        public int Length { get => bufferLength; }

        private readonly int bufferLength;
    }
}
