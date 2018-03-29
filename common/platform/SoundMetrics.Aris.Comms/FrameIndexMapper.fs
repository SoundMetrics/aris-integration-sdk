// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

[<AutoOpen>]
module private FrameIndexMapperImpl =

    type Key = RecordingId
    type Offset = int

    let addMapping entryMap recordingRequest indexOffset =
        let key = recordingRequest.Id
        let update =
            if entryMap |> Map.containsKey key then
                entryMap.[key] <> indexOffset
            else
                true

        if update then
            entryMap |> Map.add key indexOffset
        else
            entryMap

    let updateState state recordingRequest indexOffset = addMapping state recordingRequest indexOffset

    let removeMapping entryMap recordingRequest =
        entryMap |> Map.remove recordingRequest.Id

    let mapFrameIndex entryMap recordingRequest frameIndex =
        let key = recordingRequest.Id
        if entryMap |> Map.containsKey key then
            let offset = entryMap.[key]
            Some (frameIndex + offset)
        else
            None

type ReportFrameMappingFunction = RecordingRequest -> Offset -> unit

type FrameIndexMapper () =

    let state = ref Map.empty<Key, Offset>
    let guard = obj()

    member internal __.ReportFrameMapping recordingRequest indexOffset =
        lock guard (fun () ->
            state := updateState !state recordingRequest indexOffset)

    member internal __.RemoveMappingFor recordingRequest =
        lock guard (fun () -> 
            state := removeMapping !state recordingRequest)

    member __.RequestFrameMapping recordingRequest frameIndex =
        // RecordingRequest is non-nullable on the F# side, but other languages
        // may pass in null. Have to box to check for null with non-nullable types.
        if isNull (box recordingRequest) then
            None
        else
            lock guard (fun () -> 
                mapFrameIndex !state recordingRequest frameIndex)

    member x.RequestFrameMappingNullable(recordingRequest: RecordingRequest, frameIndex: int) =
        match x.RequestFrameMapping recordingRequest frameIndex with
        | Some index -> System.Nullable<int>(index)
        | None -> System.Nullable<int>()
