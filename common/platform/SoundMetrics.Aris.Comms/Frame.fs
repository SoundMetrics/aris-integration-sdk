// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Aris.FileTypes
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

//exception IndeterminateSettingsException

module Frame =

    let FrameHeaderSize = 1024
    let Epoch = DateTime(1970, 1, 1)

[<Extension>]
type ArisFrameHeaderExtensions =
    [<Extension>]
    static member IsReordered(hdr : ArisFrameHeader ref) = (!hdr).ReorderedSamples <> 0u

    [<Extension>]
    static member WindowStart(hdr : ArisFrameHeader ref) = (!hdr).WindowStart

    [<Extension>]
    static member WindowEnd(hdr : ArisFrameHeader ref) = (!hdr).WindowStart + (!hdr).WindowLength

    [<Extension>]
    static member RequestedFocus(hdr : ArisFrameHeader ref) = (!hdr).Focus

    [<Extension>]
    static member ObservedFocus(hdr : ArisFrameHeader ref) = (!hdr).FocusCurrentPosition

    [<Extension>]
    static member IsTelephoto(hdr : ArisFrameHeader ref) = (!hdr).LargeLens <> 0u

    [<Extension>]
    static member SystemType(hdr : ArisFrameHeader ref) = enum<SystemType> (int (!hdr).TheSystemType)

type Frame = {
    Header : ArisFrameHeader
    SampleData : NativeBuffer
}
with
    member f.BeamCount = uint32 f.SampleData.Length / f.Header.SamplesPerBeam // only available by calculating it

    static member HeaderFrom(buf, (timestamp: DateTimeOffset), frameIndex) =
        let h = GCHandle.Alloc(buf, GCHandleType.Pinned)
        try
            let mutable hdr =
                Marshal.PtrToStructure(h.AddrOfPinnedObject(),
                                       typeof<ArisFrameHeader>)
                    :?> ArisFrameHeader

            //-----------------------------------------------------------------
            // Set the PC timestamp. Needs to be done with DateTime rather than
            // DateTimeOffset so that 3pm DST shows up as 3pm on the topside.

            // A tick is 100ns. We need microseconds here.
            let ticks = (timestamp.DateTime - Frame.Epoch).Ticks
            let us = ticks / 10L
            hdr.sonarTimeStamp <- uint64 us

            hdr.FrameIndex <- match frameIndex with | Some fi -> fi | None -> hdr.FrameIndex

            hdr
        finally
            h.Free()
