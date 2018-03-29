// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.NativeInterop
open System
open System.Runtime.InteropServices

// For native pointers:
// warning FS0009: Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn "9"'.
#nowarn "9"

module private NativeMemoryDetails =

    //-------------------------------------------------------------------------
    // This allows cross-platform execution of native memory copies.
    // Windows uses the CopyMemory API; others differ.
    //
    // More on native p/invoke across platforms
    //-----------------------------------------
    // Discussion of the topic in the dotnet coreclr repo:
    // https://github.com/dotnet/coreclr/issues/930
    //
    // A discussion of the Marshaling Code Generator (MCG) exists here:
    // https://blogs.msdn.microsoft.com/dotnet/2014/05/09/the-net-native-tool-chain/
    //-------------------------------------------------------------------------

    type CopyMemoryFn = (nativeptr<byte> * nativeptr<byte> * int) -> unit

    [<Sealed>]
    type CopyMemoryWindows private () =
        [<DllImport("kernel32.dll", EntryPoint="CopyMemory")>]
        static extern void private CopyMemoryWin(byte* _destination, byte* _source, int32 _size);

        static member CopyMemory(destination : nativeptr<byte>,
                                 source : nativeptr<byte>,
                                 length : int) : unit =
            CopyMemoryWin(destination, source, length)

    let copyMemoryFn : Lazy<CopyMemoryFn> = lazy (
        match Environment.OSVersion.Platform with
        | PlatformID.Win32NT -> CopyMemoryWindows.CopyMemory
        | platformId -> failwithf "Unsupported platform: %A" platformId)

    let CopyMemory(destination : nativeptr<byte>,
                   source : nativeptr<byte>,
                   length : int) : unit =
        let copy = copyMemoryFn.Value
        copy(destination, source, length)
        copyMemoryFn.Value(destination, source, length)

open NativeMemoryDetails

module private NativeBufferImpl =

    let allocBuffer (length : uint32) : nativeptr<byte> =
        Marshal.AllocHGlobal(int length) |> NativePtr.ofNativeInt<byte>

    let copyByteArrayToBuffer (source : byte array) (buffer : nativeptr<byte>) =
        
        use bytes = fixed source
        CopyMemory(buffer, bytes, source.Length)

    let copyByteArraysToBuffer (arrays : (uint32 * byte array) seq) (buffer : nativeptr<byte>) =
        
        for (offset, data) in arrays do
            use source = fixed data
            let dest = NativePtr.add<byte> buffer (int offset)
            CopyMemory(dest, source, data.Length)

    let byteArrayToNative (bytes : byte array) =
        let length = uint32 bytes.Length
        let buffer = allocBuffer length

        copyByteArrayToBuffer bytes buffer
        (buffer, length)

    let byteArraysToNative (arrays : (uint32 * byte array) seq) =

        let cached = arrays |> Seq.cache

        if cached |> Seq.isEmpty then
            (allocBuffer 0u, 0u)
        else
            let maxOffsetFragment = cached |> Seq.maxBy (fun (offset, _data) -> offset)
            let totalLength = fst maxOffsetFragment + uint32 (snd maxOffsetFragment).Length

            let buffer = allocBuffer totalLength
            copyByteArraysToBuffer cached buffer
            (buffer, totalLength)


open NativeBufferImpl


/// Immutable buffer, backed by native memory in order to avoid the LOH.
[<Sealed>]
type NativeBuffer private (source : nativeptr<byte>, length : uint32) as self =

    let mutable disposed = false

    let buffer =
        if source = NativePtr.ofNativeInt(nativeint 0) then
            invalidArg "source" "must not be null"

        if length = 0u then
            invalidArg "length" "cannot be zero"

        Marshal.AllocHGlobal(int length)

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
        Marshal.FreeHGlobal(buffer)

    interface IDisposable with
        member s.Dispose() = dispose true

    member __.Dispose() = dispose true
    override __.Finalize() = dispose false

    member __.Length = length
    member private __.Buffer = buffer

    member __.Transform(f : (nativeint * nativeint) -> unit) : NativeBuffer =

        let source = buffer
        let destination = new NativeBuffer(allocBuffer length, length)
        f(source, destination.Buffer)
        destination

    member __.Read(f : nativeint -> unit) : unit = f(buffer)

    member __.ToArray() =

        let arr = Array.zeroCreate<byte> (int length)
        Marshal.Copy(buffer, arr, 0, int length)
        arr


    // Only the secondary constructors allocate a buffer, the primary does not.

    static member FromByteArray(bytes : byte array) : NativeBuffer =
        new NativeBuffer(byteArrayToNative bytes)

    /// Copies sample data into an immutable container. This allows for a sparsely-populated
    /// buffer.
    static member FromByteArrays(fragments: (uint32 * byte array) seq) : NativeBuffer =

        new NativeBuffer(byteArraysToNative fragments)

module NativeBuffer = 

    /// Transforms into a new copy; assumes the output is the same size as the source.
    let transform f (source : NativeBuffer) : NativeBuffer = source.Transform(f)

    let iter f (source : NativeBuffer) : unit = source.Read(f)
