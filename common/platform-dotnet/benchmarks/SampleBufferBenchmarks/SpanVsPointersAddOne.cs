using BenchmarkDotNet.Attributes;
using System.Runtime.InteropServices;

namespace SampleBufferBenchmarks
{
    public class SpanVsPointersAddOne : IDisposable
    {
        private IntPtr buffer;
        private int bufferSize;

        private Span<byte> BufferSpan
        {
            get { unsafe { return new Span<byte>(buffer.ToPointer(), bufferSize); }; }
        }

        public SpanVsPointersAddOne()
        {
            bufferSize = 10_000_000;
            buffer = Marshal.AllocHGlobal(bufferSize);
        }

        [Benchmark]
        public void SpanAddOne()
        {
            var span = BufferSpan;
            for (int i = 0; i < span.Length; i++)
            {
                span[i] += 1;
            }
        }

        [Benchmark]
        public unsafe void PointerAddOne()
        {
            byte* pb = (byte*)buffer.ToPointer();
            byte* pend = pb + bufferSize;

            while (pb < pend)
            {
                *pb += 1;
                ++pb;
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(buffer);
            buffer = IntPtr.Zero;
            bufferSize = 0;
        }
    }
}
