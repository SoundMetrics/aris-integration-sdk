using System;
using System.IO;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    using static Math;

    internal sealed class NativeBufferStream : Stream
    {
        private readonly NativeBuffer inputBuffer;
        private readonly NativeBuffer outputBuffer;
        private readonly TransformFunction transformFunction;
        private readonly int transformScale;
        private readonly long length;
        private readonly IntPtr inputBufferBegin, inputBufferEnd;
        private readonly IntPtr outputBufferBegin, outputBufferEnd;

        #region Read Buffer Management

        private long position; // Backer for Stream.Position
        private IntPtr inputBufferCursor, outputBufferCursor;

        #endregion

        public delegate void TransformFunction(
            int blockSize,
            IntPtr source,
            IntPtr destination);

        public NativeBufferStream(
            NativeBuffer inputBuffer,
            TransformFunction transformFunction,
            int transformScale)
        {
            this.inputBuffer = inputBuffer;
            this.transformFunction = transformFunction;
            this.transformScale = transformScale;
            this.length = inputBuffer.Length * transformScale;

            inputBufferBegin = inputBuffer.UnderlyingHandle.Handle;
            inputBufferEnd = inputBufferBegin + inputBuffer.Length;
            inputBufferCursor = inputBufferEnd;

            this.outputBuffer = new NativeBuffer(inputBuffer.Length * transformScale);
            outputBufferBegin = outputBuffer.UnderlyingHandle.Handle;
            outputBufferEnd = outputBufferBegin + outputBuffer.Length;
            outputBufferCursor = outputBufferEnd;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => position;
            set => throw new System.NotImplementedException();
        }

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                outputBuffer.Dispose();
            }

            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return 0;
            }

            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            // Transforms the native buffer on-the-fly.
            throw new System.NotImplementedException();

            ArraySegment<byte> Consume(ArraySegment<byte> segment, int amount)
            {
                if (segment.Count < amount)
                {
                    throw new Exception("amount > segment.Count");
                }

                return new ArraySegment<byte>(
                    segment.Array,
                    segment.Offset + amount,
                    segment.Count - amount
                    );
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }
    }
}
