using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    /// <summary>
    /// Immutable buffer, backed by native memory in order to avoid the LOH.
    /// </summary>
    public sealed class NativeBuffer : IDisposable
    {
        public NativeBuffer(ArraySegment<byte> contents)
        {
            if (contents.Array == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            hNativeBuffer = new NativeBufferHandle(contents);
        }

        public NativeBuffer(IEnumerable<ArraySegment<byte>> contents)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            hNativeBuffer = new NativeBufferHandle(contents);
        }

        internal NativeBuffer(int length)
        {
            hNativeBuffer = new NativeBufferHandle(length);
        }

        public int Length { get => hNativeBuffer.Length; }

        public byte[] ToManagedArray()
        {
            var result = new byte[hNativeBuffer.Length];
            Marshal.Copy(hNativeBuffer.Handle, result, 0, result.Length);
            return result;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // hNativeBuffer has its own finalizer
                    hNativeBuffer.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion

        private readonly NativeBufferHandle hNativeBuffer;
    }
}

/*
/// Immutable buffer, backed by native memory in order to avoid the LOH.
[<Sealed>]
type NativeBuffer private (buffer : NativeBufferHandle) as self =

    let mutable disposed = false

    let checkDisposed () =
        if disposed then
            raise (ObjectDisposedException("NativeBuffer"))

    let dispose isDisposing =

        if isDisposing then
            // Clean up managed resources
            checkDisposed()
            disposed <- true
            GC.SuppressFinalize(self)

        // Clean up native resources
        buffer.Dispose()


    interface IDisposable with
        member s.Dispose() = dispose true

    member __.Dispose() = dispose true
    override __.Finalize() = dispose false

    member __.Length = buffer.Length

    member __.TransformFrame(txf : TransformFunction,
                             pingMode : uint32,
                             pingsPerFrame : uint32,
                             beamCount : uint32,
                             samplesPerBeam : uint32) : NativeBuffer =

        let destination = NativeBufferHandle.Create buffer.Length
        let source = buffer
        txf.Invoke(pingMode, pingsPerFrame, beamCount, samplesPerBeam, source.IntPtr, destination.IntPtr)
        new NativeBuffer(destination)

    member __.Map<'Result>(f : (nativeptr<byte> * int) -> 'Result) : 'Result = f(buffer.NativePtr, buffer.Length)

    member __.ToArray() = bufferToByteArray buffer


    // Only the secondary constructors allocate a buffer, the primary does not.

    static member FromByteArray(bytes : byte array) : NativeBuffer =
        new NativeBuffer(byteArrayToBuffer bytes)

    /// Copies sample data into an immutable container. This allows for a sparsely-populated
    /// buffer. The tuple contains an offset into the destination and a byte array.
    static member FromByteArrays(fragments: (int * byte array) seq) : NativeBuffer =

        new NativeBuffer(byteArraysToNative fragments)
 */
