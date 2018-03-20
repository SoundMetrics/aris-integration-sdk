// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.NativeInterop
open System
open System.Runtime.InteropServices

// For native pointers:
// warning FS0009: Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn "9"'.
#nowarn "9"

module private NativeMemoryImpl =

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

open NativeMemoryImpl

module private NativeBufferImpl =

    let allocBuffer length : nativeptr<byte> = Marshal.AllocHGlobal(int length) |> NativePtr.ofNativeInt<byte>

    let copyByteArrayToBuffer (source : byte array) (buffer : nativeptr<byte>) =
        
        use bytes = fixed source
        CopyMemory(buffer, bytes, source.Length)

    let copyByteArraysToBuffer (arrays : (byte array) seq) (buffer : nativeptr<byte>) =
        
        let mutable offset = 0
        for arr in arrays do
            use source = fixed arr
            let dest = NativePtr.add<byte> buffer offset
            CopyMemory(dest, source, arr.Length)
            offset <- offset + arr.Length

    let byteArrayToNative (bytes : byte array) =
        let length = bytes.Length
        let buffer = allocBuffer length

        copyByteArrayToBuffer bytes buffer
        (buffer, length)

    let byteArraysToNative (arrays : (byte array) seq) =
        let cached = arrays |> Seq.cache
        let totalLength = cached |> Seq.sumBy (fun bytes -> bytes.Length)

        let buffer = allocBuffer totalLength
        copyByteArraysToBuffer cached buffer
        (buffer, totalLength)


open NativeBufferImpl


/// Immutable buffer, backed by native memory in order to avoid the LOH.
[<Sealed>]
type NativeBuffer private (source : nativeptr<byte>, length : int) as self =

    let mutable disposed = false

    let buffer =
        if source = NativePtr.ofNativeInt(nativeint 0) then
            invalidArg "source" "must not be null"

        if length = 0 then
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

    do
        // Only the secondary constructors allocate a buffer, the primary does not.

        if source = NativePtr.ofNativeInt(nativeint 0) then
            invalidArg "source" "must not be null"

        if length = 0 then
            invalidArg "length" "cannot be zero"

    interface IDisposable with
        member s.Dispose() = dispose true

    member __.Dispose() = dispose true
    override __.Finalize() = dispose false

    static member FromByteArray(bytes : byte array) : NativeBuffer =
        new NativeBuffer(byteArrayToNative bytes)

    static member FromByteArrays(arrays : (byte array) seq) : NativeBuffer =

        new NativeBuffer(byteArraysToNative arrays)
