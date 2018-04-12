// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.NativeInterop
open System

// For native pointers:
// warning FS0009: Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn "9"'.
#nowarn "9"

type Histogram = { values: int[] }
with
    static member Create () = { values = Array.zeroCreate<int> 256 }

    /// Crass attempt to do something faster than using managed arrays (much).
    member internal s.CreateUpdater () =
        let h = Runtime.InteropServices.GCHandle.Alloc(s.values, Runtime.InteropServices.GCHandleType.Pinned)
        let dispose = fun () -> h.Free()
        let addr = h.AddrOfPinnedObject()
        let basePtr = NativePtr.ofNativeInt<int> addr
        let incr = fun value ->
                    let p = NativePtr.add basePtr value
                    let current = NativePtr.read p
                    NativePtr.write p (current + 1)
        incr, dispose
