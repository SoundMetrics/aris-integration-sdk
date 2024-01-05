using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SampleBufferBenchmarks
{
    public class SpanVsPointersBoxFilter : IDisposable
    {
        private const int Rows = 1_000;
        private const int Columns = 1_000;

        private IntPtr inputBuffer, outputBuffer;
        private int bufferSize;

        private unsafe Span<byte> InputBufferSpan => new Span<byte>(inputBuffer.ToPointer(), bufferSize);

        private unsafe Span<byte> OutputBufferSpan => new Span<byte>(outputBuffer.ToPointer(), bufferSize);

        public SpanVsPointersBoxFilter()
        {
            AssertValidity();

            const int BufferSize = Rows * Columns;

            (inputBuffer, bufferSize) = AllocateBuffer(BufferSize);
            (outputBuffer, _) = AllocateBuffer(BufferSize);
        }

        [Benchmark]
        public void SpanBoxFilter()
        {
            SpanBoxFilterCore(InputBufferSpan, OutputBufferSpan, Rows, Columns);
        }

        private static void SpanBoxFilterCore(ReadOnlySpan<byte> input, Span<byte> output, int rows, int columns)
        {
            // Implements a naive box filter, skipping the edge cases.
            // This operation transforms a source buffer into a destination buffer.

            // Span<T> is 1-dimension, work around that as if it were 2
            // (iterate from 1 to N-1 on each dimension).
            // Assume there is no padding or alignment in the data structures.

            // Cells around the current position are labeled 1-9, from the left-top
            // numbered clockwise.

            for (int row = 1; row < rows - 1; row++)
            {
                int previousRowOffset = (row - 1) * columns + 1;
                int currentRowOffset = (row + 0) * columns + 1;
                int nextRowOffset = (row + 1) * columns + 1;

                for (int col = 1; col < columns - 1; col++)
                {
                    var sum =
                        input[previousRowOffset - 1] + input[previousRowOffset + 0] + input[previousRowOffset + 1]
                        + input[currentRowOffset - 1] + input[currentRowOffset + 0] + input[currentRowOffset + 1]
                        + input[nextRowOffset - 1] + input[nextRowOffset + 0] + input[nextRowOffset + 1];

                    output[currentRowOffset] = (byte)(sum / 9);

                    ++previousRowOffset;
                    ++currentRowOffset;
                    ++nextRowOffset;
                }
            }
        }

        [Benchmark,
            Arguments(-1),
            Arguments(2),
            Arguments(4),
            Arguments(8),
            Arguments(16)]
        public void SpanBoxFilterParallel(int maxDegreeOfParallelism)
        {
            SpanBoxFilterParallelCore(InputBufferSpan, OutputBufferSpan, Rows, Columns, maxDegreeOfParallelism);
        }

        private unsafe static void SpanBoxFilterParallelCore(
            ReadOnlySpan<byte> input,
            Span<byte> output,
            int rows,
            int columns,
            int maxDegreeOfParallelism = -1)
        {
            var parellelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            // Implements a naive box filter, skipping the edge cases.
            // This operation transforms a source buffer into a destination buffer.

            // Span<T> is 1-dimension, work around that as if it were 2
            // (iterate from 1 to N-1 on each dimension).
            // Assume there is no padding or alignment in the data structures.

            // Cells around the current position are labeled 1-9, from the left-top
            // numbered clockwise.

            // We cannot capture a Span<T>, but we can turn it into a fixed pointer
            // that we capture, then convert the fixed pointer to a span in the loop
            // action.
            fixed (byte* pInput = input)
            fixed (byte* pOutput = output)
            {
                var pIn = pInput; // Chicanery: the compiler thinks we've considered the consequences.
                var pOut = pOutput; // Chicanery: the compiler thinks we've considered the consequences.

                // Consequences: otherwise, we couldn't use the pointer to recreate the span, which we
                // cannot pass in the lambda capture.

                Parallel.For(1, rows - 1, parellelOptions, row =>
                {
                    var input2 = new Span<byte>(pIn, rows * columns);
                    var output2 = new Span<byte>(pOut, rows * columns);

                    ProcessRow(row, input2, output2);
                });

                void ProcessRow(int row, ReadOnlySpan<byte> input, Span<byte> output)
                {
                    int previousRowOffset = (row - 1) * columns + 1;
                    int currentRowOffset = (row + 0) * columns + 1;
                    int nextRowOffset = (row + 1) * columns + 1;

                    for (int col = 1; col < columns - 1; col++)
                    {
                        var sum =
                            input[previousRowOffset - 1] + input[previousRowOffset + 0] + input[previousRowOffset + 1]
                            + input[currentRowOffset - 1] + input[currentRowOffset + 0] + input[currentRowOffset + 1]
                            + input[nextRowOffset - 1] + input[nextRowOffset + 0] + input[nextRowOffset + 1];

                        output[currentRowOffset] = (byte)(sum / 9);

                        ++previousRowOffset;
                        ++currentRowOffset;
                        ++nextRowOffset;
                    }
                }
            }
        }

        [Benchmark]
        public unsafe void PointerBoxFilter()
        {
            PointerBoxFilterCore(inputBuffer, outputBuffer, Rows, Columns);
        }

        private unsafe static void PointerBoxFilterCore(IntPtr inputBuffer, IntPtr outputBuffer, int rows, int columns)
        {
            // Implements a naive box filter, skipping the edge cases.
            // This operation transforms a source buffer into a destination buffer.

            // Span<T> is 1-dimension, work around that as if it were 2
            // (iterate from 1 to N-1 on each dimension).
            // Assume there is no padding or alignment in the data structures.

            // Cells around the current position are labeled 1-9, from the left-top
            // numbered clockwise.

            byte* pInputBuffer = (byte*)inputBuffer.ToPointer();
            byte* pOutputBuffer = (byte*)outputBuffer.ToPointer();

            for (int row = 1; row < rows - 1; row++)
            {
                byte* previousRow = pInputBuffer + (row - 1) * columns + 1;
                byte* currentRow = pInputBuffer + (row + 0) * columns + 1;
                byte* nextRow = pInputBuffer + (row + 1) * columns + 1;

                for (int col = 1; col < columns - 1; col++)
                {
                    var sum =
                        *(previousRow - 1) + *(previousRow + 0) + *(previousRow + 1)
                        + *(currentRow - 1) + *(currentRow + 0) + *(currentRow + 1)
                        + *(nextRow - 1) + *(nextRow + 0) + *(nextRow + 1);

                    pOutputBuffer[row * columns + col] = (byte)(sum / 9);

                    ++previousRow;
                    ++currentRow;
                    ++nextRow;
                }
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(inputBuffer);
            Marshal.FreeHGlobal(outputBuffer);
            inputBuffer = IntPtr.Zero;
            outputBuffer = IntPtr.Zero;
            bufferSize = 0;
        }

        private static (IntPtr buffer, int size) AllocateBuffer(int size) => (Marshal.AllocHGlobal(size), size);

        private static void AssertValidity()
        {
            const int Rows = 4;
            const int Columns = 5;

            var (generatedInput, expected, expectedDescription) = CreateInputAndExpected();

            AssertSpanWorks();
            AssertSpanParallelWorks();
            AssertPointerWorks();

            Console.WriteLine($"// ### {nameof(AssertValidity)} passed.");

            void AssertSpanWorks()
            {
                var output = new byte[expected.Length];

                SpanBoxFilterCore(generatedInput.Span, output, Rows, Columns);
                AssertAreEqual(expected.Span, output, nameof(AssertSpanWorks));
            }

            void AssertSpanParallelWorks()
            {
                var output = new byte[expected.Length];

                SpanBoxFilterParallelCore(generatedInput.Span, output, Rows, Columns);
                AssertAreEqual(expected.Span, output, nameof(AssertSpanWorks));
            }

            unsafe void AssertPointerWorks()
            {
                var (inputBuffer, size) = AllocateBuffer(Rows * Columns);
                var (outputBuffer, _) = AllocateBuffer(Rows * Columns);

                var inputSpan = new Span<byte>(inputBuffer.ToPointer(), size);
                generatedInput.Span.CopyTo(inputSpan);
                new Span<byte>(outputBuffer.ToPointer(), size).Clear();


                try
                {
                    PointerBoxFilterCore(inputBuffer, outputBuffer, Rows, Columns);
                    var outputSpan = new Span<byte>(outputBuffer.ToPointer(), size);
                    AssertAreEqual(expected.Span, outputSpan, nameof(AssertPointerWorks));
                }
                finally
                {
                    Marshal.FreeHGlobal(inputBuffer);
                    Marshal.FreeHGlobal(outputBuffer);
                }
            }

            static void AssertAreEqual(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, string description)
            {
                if (!expected.SequenceEqual(actual))
                {
                    var buffer = new StringBuilder();
                    buffer.AppendLine($"Result is not as expected; validation failed. [{description}]");
                    buffer.Append("expected: ");
                    buffer.AppendLine(CreateDescription(expected));
                    buffer.Append("actual: ");
                    buffer.AppendLine(CreateDescription(actual));

                    throw new Exception(buffer.ToString());
                }
            }

            static (ReadOnlyMemory<byte> Input, ReadOnlyMemory<byte> Expected, string ExpectedDescription)
                CreateInputAndExpected()
            {
                ReadOnlyMemory<byte> input = new byte[]
                {
                    10, 20, 30, 40, 50,
                    60, 65, 70, 75, 80,
                    85, 92, 99, 106, 113,
                    122, 131, 140, 149, 158,
                };

                ReadOnlyMemory<byte> expected = new byte[]
                {
                    0, 0, 0, 0, 0,
                    0, 59, 66, 73, 0,
                    0, 96, 103, 110, 0,
                    0, 0, 0, 0, 0,
                };

                return (input, expected, CreateDescription(expected.Span));
            }

            static string CreateDescription(ReadOnlySpan<byte> values)
            {
                var buffer = new StringBuilder();

                for (int row = 0; row < Rows; row++)
                {
                    var rowValues = values.Slice(row * Columns, Columns);
                    buffer.AppendLine(string.Join(", ", rowValues.ToArray()));
                }

                return buffer.ToString();
            }
        }
    }
}
