// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System

open SoundMetrics.NativeMemory

/// Accumulates multiple packets which make up a frame.
/// Mutable.
/// Contents and frame index may change.
/// Does not track missing frame segments.
[<Sealed(true)>]
type internal FrameAccumulator(frameIndex : FrameIndex,
                               headerBytes : byte array,
                               timestamp : DateTimeOffset,
                               firstDataFragment : byte array,
                               totalDataSize : uint32) =
    let mutable fi = frameIndex

    [<Literal>]
    let TypicalMaxFragmentCount = 200 // more than enough for 128 beams, 2000 samples.
    let fragments = ResizeArray<int * byte array>(capacity = 200)

    let mutable dataReceived = 0u

    let validateInputs () =
        if frameIndex < 0 then invalidArg "frameIndex" "must be >= zero"
        if headerBytes.Length = 0 then invalidArg "headerBytes.Length" "must be > zero"

    let appendFrameData (dataOffset: uint32) (dataFragment: byte[]) =
        // The sliding window code elsewhere deals with whether we succeed or
        // fail on missing data; here we just store it away.
        fragments.Add((int dataOffset, dataFragment))
        dataReceived <- dataReceived + uint32 dataFragment.Length

    do
        validateInputs ()
        appendFrameData 0u firstDataFragment

    member __.SetFrameIndex newFrameIndex = fi <- newFrameIndex

    member __.FrameIndex = fi
    member __.FrameReceiptTimestamp = timestamp
    member __.IsComplete = (dataReceived = totalDataSize)
    member __.BytesReceived = dataReceived
    member __.ExpectedSize = totalDataSize

    member __.HeaderBytes = headerBytes

    /// Copies sample data into an immutable container.
    member __.SampleData = NativeBuffer.FromByteArrays(fragments)

    member __.AppendFrameData (dataOffset: uint32) (dataFragment: byte[]) =
        appendFrameData dataOffset dataFragment
