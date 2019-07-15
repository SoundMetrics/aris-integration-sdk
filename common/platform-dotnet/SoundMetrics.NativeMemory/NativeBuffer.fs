// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.NativeMemory

open Microsoft.FSharp.NativeInterop
open System
open System.Runtime.InteropServices

// For native pointers:
// warning FS0009: Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn "9"'.
#nowarn "9"

module private NativeBufferDetails =
    open Microsoft.Win32.SafeHandles

    type NativeBufferHandle private (hBuffer : IntPtr, length) =
        inherit SafeHandleZeroOrMinusOneIsInvalid(true)

        do
            base.SetHandle(hBuffer)

        member b.IntPtr = b.DangerousGetHandle()
        member b.NativePtr = NativePtr.ofNativeInt<byte> b.IntPtr
        member __.Length = length

        override b.ReleaseHandle() =
            Marshal.FreeHGlobal(b.IntPtr)
            true

        static member Create length =
            new NativeBufferHandle(Marshal.AllocHGlobal(length : int), length)

        static member private EmptyBuffer = lazy (new NativeBufferHandle(Marshal.AllocHGlobal(0), 0))
        static member Empty = NativeBufferHandle.EmptyBuffer.Value

    let copyMemory (destination : nativeptr<byte>)
                   (source : nativeptr<byte>)
                   (length : int)
                   : unit =
        // Class `Marshal` does not provide a native-to-native Copy function, so
        // for the moment do the copy manually. The intent here is to provide a
        // portable "copy memory" function that does not require writing C or C++
        // code for each supported platform.

        // Copy 4 bytes at a time for speed, then get the remainder done.
        let fourByteCount = length / 4
        let mutable fourByteSrc =  NativePtr.ofNativeInt<int32> (NativePtr.toNativeInt source)
        let mutable fourByteDest = NativePtr.ofNativeInt<int32> (NativePtr.toNativeInt destination)
        for i = 0 to fourByteCount - 1 do
            NativePtr.write fourByteDest (NativePtr.read fourByteSrc)
            fourByteSrc <-  NativePtr.add fourByteSrc  1
            fourByteDest <- NativePtr.add fourByteDest 1

        let oneByteCount = length % 4
        let mutable oneByteSrc =  NativePtr.ofNativeInt<byte> (NativePtr.toNativeInt source)
        let mutable oneByteDest = NativePtr.ofNativeInt<byte> (NativePtr.toNativeInt destination)
        for i = 0 to oneByteCount - 1 do
            NativePtr.write oneByteDest (NativePtr.read oneByteSrc)
            oneByteSrc <-  NativePtr.add oneByteSrc  1
            oneByteDest <- NativePtr.add oneByteDest 1

    // Mutates `buffer`
    let copyByteArrayToBuffer (source : byte array) (buffer : NativeBufferHandle) =

        // Here we can use `Marshal` to do the work.
        let length = source.Length
        let buffer = NativeBufferHandle.Create length
        Marshal.Copy(source, 0, buffer.IntPtr, length)

    // Mutates `buffer`
    let copyByteArraysToBuffer (arrays : (int * byte array) seq) (buffer : NativeBufferHandle) =

        // Here we can use `Marshal` to do the work.
        let buffer' = NativePtr.ofNativeInt<byte> buffer.IntPtr
        for (offset, source) in arrays do
            let length = source.Length
            let dest = NativePtr.toNativeInt (NativePtr.add buffer' offset)
            Marshal.Copy(source, 0, dest, length)

    let byteArrayToBuffer (bytes : byte array) =
        let length = bytes.Length
        let buffer = NativeBufferHandle.Create length

        copyByteArrayToBuffer bytes buffer
        buffer

    let byteArraysToNative (arrays : (int * byte array) seq) =

        let cached = arrays |> Seq.cache

        if cached |> Seq.isEmpty then
            NativeBufferHandle.Empty
        else
            let maxOffsetFragment = cached |> Seq.maxBy (fun (offset, _data) -> offset)
            let totalLength = fst maxOffsetFragment + (snd maxOffsetFragment).Length

            let buffer = NativeBufferHandle.Create totalLength
            copyByteArraysToBuffer cached buffer
            buffer

    let bufferToByteArray (buffer : NativeBufferHandle) =

        let arr = Array.zeroCreate<byte> (buffer.Length)
        Marshal.Copy(buffer.IntPtr, arr, 0, buffer.Length)
        arr


open NativeBufferDetails

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

        // Clean up native resources
        buffer.Dispose()


    interface IDisposable with
        member s.Dispose() = dispose true

    member __.Dispose() =
        dispose true
        GC.SuppressFinalize(self)

    override __.Finalize() = dispose false

    member __.Length = buffer.Length

    member __.Transform(txf : Action<nativeint,nativeint>) : NativeBuffer =

        let destination = NativeBufferHandle.Create buffer.Length
        let source = buffer
        txf.Invoke(source.IntPtr, destination.IntPtr)
        new NativeBuffer(destination)

    member __.UpscaleTransformToBuffer(txf : Action<nativeint,nativeint>,
                                       destination : nativeint) : unit =

        txf.Invoke(buffer.IntPtr, destination)

    member me.UpscaleTransform(txf : Action<nativeint,nativeint>, scale : int) : NativeBuffer =

        let newBufferSize = buffer.Length * scale
        let destination = NativeBufferHandle.Create newBufferSize
        me.UpscaleTransformToBuffer(txf, destination.IntPtr)
        new NativeBuffer(destination)

    member __.CopyTo(destination : nativeint, length : int) =
        if length <> buffer.Length then
            invalidArg "length" "Invalid length specified"

        let destination' = NativePtr.ofNativeInt<byte> destination
        let source = buffer.NativePtr
        copyMemory destination' source length

    member __.Map<'Result>(f : Func<nativeptr<byte>,int,'Result>) : 'Result =

        f.Invoke(buffer.NativePtr, buffer.Length)

    member __.ToArray() = bufferToByteArray buffer


    // Only the secondary constructors allocate a buffer, the primary does not.

    static member FromByteArray(bytes : byte array) : NativeBuffer =
        new NativeBuffer(byteArrayToBuffer bytes)

    /// Copies sample data into an immutable container. This allows for a sparsely-populated
    /// buffer. The tuple contains an offset into the destination and a byte array.
    static member FromByteArrays(fragments: (int * byte array) seq) : NativeBuffer =

        new NativeBuffer(byteArraysToNative fragments)

module NativeBuffer =

    /// Transforms into a new copy; assumes the output is the same size as the source.
    let transform (txf : nativeint -> nativeint -> unit) (source : NativeBuffer)
        : NativeBuffer =

        let txf' = Action<nativeint,nativeint>(txf)
        source.Transform(txf')

    let map<'Result> (f : (nativeptr<byte> -> int -> 'Result)) (source : NativeBuffer) : 'Result =
            let f' = Func<nativeptr<byte>,int,'Result>(f)
            source.Map(f')
