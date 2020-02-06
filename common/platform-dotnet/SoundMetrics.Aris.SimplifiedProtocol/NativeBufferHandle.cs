using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    [DebuggerDisplay("{Handle} {ShortString}")]
    public sealed class NativeBufferHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public NativeBufferHandle(int bufferLength)
            : base(ownsHandle: true)
        {
            var hBuffer =
                Marshal.AllocHGlobal(ValidateLength(bufferLength));
            SetHandle(hBuffer);
            this.bufferLength = bufferLength;
            InitializeBuffer(DangerousGetHandle(), bufferLength, 0xCC);
        }

        private static int ValidateLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return length;
        }

        public void Append(ArraySegment<byte> contents)
        {
            var remainingBuffer = bufferLength - position;

            if (contents.Count > remainingBuffer)
            {
                throw new ArgumentException(
                    "Cannot append contents, it is too long",
                    nameof(contents));
            }

            var source = contents.Array;
            var startIndex = contents.Offset;
            var ptr = DangerousGetHandle() + position;

            Marshal.Copy(source, startIndex, ptr, contents.Count);

            if (position == 0) // TODO REMOVE
            {
                Debug.WriteLine($"Started {ShortString}");
            }

            position += contents.Count;
        }

        protected override bool ReleaseHandle()
        {
            InitializeBuffer(DangerousGetHandle(), bufferLength, 0xDD);
            Marshal.FreeHGlobal(DangerousGetHandle());
            return true;
        }

        // Test support.
        internal byte[] ToManagedArray()
        {
            var result = new byte[Length];
            Marshal.Copy(DangerousGetHandle(), result, 0, result.Length);
            return result;
        }

        public byte[] ToManagedArray(int length)
        {
            var result = new byte[length];
            Marshal.Copy(DangerousGetHandle(), result, 0, length);
            return result;
        }


        public int Length { get => bufferLength; }

        internal IntPtr Handle { get => DangerousGetHandle(); }

        public string ShortString
        {
            get {
                var elementCount = Math.Min(5, bufferLength);
                var bytes = ToManagedArray(elementCount);
                var elements = String.Join(" ", bytes);

                return $"[{elements}...({DangerousGetHandle()})]";
            }
        }

        [Conditional("DEBUG")]
        private static unsafe void InitializeBuffer(IntPtr buffer, int length, byte value)
        {
            var pBuffer = (byte*)buffer.ToPointer();
            var pEnd = pBuffer + length;

            for (; pBuffer < pEnd; ++pBuffer)
            {
                *pBuffer = value;
            }
        }

        private readonly int bufferLength;
        private int position;
    }
}
