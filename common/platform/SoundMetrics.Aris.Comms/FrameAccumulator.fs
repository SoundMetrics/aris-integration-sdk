// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.NativeInterop
open System
open System.Runtime.InteropServices

// For native pointers:
// warning FS0009: Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn "9"'.
#nowarn "9"

/// Accumulates multiple packets which make up a frame.
/// Mutable.
/// Contents and frame index may change.
/// Does not track missing frame segments.
[<Sealed(true)>]
type internal FrameAccumulator(frameIndex : FrameIndex,
                               headerBytes : byte array,
                               timestamp : DateTimeOffset,
                               firstDataFragment : byte array,
                               totalDataSize : int) =
    let mutable fi = frameIndex

    [<Literal>]
    let TypicalMaxFragmentCount = 200 // more than enough for 128 beams, 2000 samples.
    let fragments = ResizeArray<int * byte array>(capacity = 200)

    let mutable dataReceived = 0

    let validateInputs () =
        if frameIndex < 0 then invalidArg "frameIndex" "must be >= zero"
        if headerBytes.Length = 0 then invalidArg "headerBytes.Length" "must be > zero"

    let appendFrameData (dataOffset: int) (dataFragment: byte[]) =
        // The sliding window code elsewhere deals with whether we succeed or
        // fail on missing data; here we just store it away.
        fragments.Add((dataOffset, dataFragment))
        dataReceived <- dataReceived + dataFragment.Length

    do
        validateInputs ()
        appendFrameData 0 firstDataFragment

    member __.SetFrameIndex newFrameIndex = fi <- newFrameIndex

    member __.FrameIndex = fi
    member __.FrameReceiptTimestamp = timestamp
    member __.IsComplete = (dataReceived = totalDataSize)
    member __.BytesReceived = dataReceived
    member __.ExpectedSize = totalDataSize

    member __.HeaderBytes = headerBytes

    /// Copies sample data into an immutable container.
    member __.SampleData = NativeBuffer.FromByteArrays(fragments)

    member __.AppendFrameData (dataOffset: int) (dataFragment: byte[]) =
        appendFrameData dataOffset dataFragment
