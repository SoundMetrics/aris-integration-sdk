// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System

//-----------------------------------------------------------------------------
// Recording

type RecordingNamingPolicyInput = {
    RecordingTime: DateTime
    BeamCount: uint32
    SampleCount: uint32
}

type RecordingFailure = {
    FailedPath : string option
}
with
    static member None = { FailedPath = None }

type RecordingPath = string
type RecordingPathFactory = System.Func<RecordingNamingPolicyInput, RecordingFailure, bool * RecordingPath>
type RecordingId = Guid

type RecordingTerminationReason =
| RecordingComplete
| RecordingError of errors : string list
| RecordingNoMoreOptions of errors : string list // App couldn't provide a new place to record.

/// Request to start or stop recording; when stopping a recording, the original
/// request must be used to identify the recording to be stopped. Do not assume
/// there is only one recording in flight for one sonar.
type RecordingRequest = {
    Id: RecordingId // Allows for easy comparison.
    Description: string
    GetRecordingPath: RecordingPathFactory
    OnTermination: RecordingTerminationHandler
    JobDisplayTime: DateTime option
}
with
    static member Create(description, getRecordingPath, onTermination) =
        { Id = Guid.NewGuid()
          Description = description
          GetRecordingPath = getRecordingPath
          OnTermination = onTermination
          JobDisplayTime = None }

    member internal r.BuildRecordingPath (now: DateTime) beamCount sampleCount recordingFailure =

        // Protect ourselves across this call to application level code--don't let any exceptions
        // leak through, turn it into a Choice.
        try
            let success, path =
                r.GetRecordingPath.Invoke ({ RecordingTime = now; BeamCount = beamCount; SampleCount = sampleCount },
                                           recordingFailure)
            if success then
                let folder = IO.Path.GetDirectoryName(path)
                IO.Directory.CreateDirectory(folder) |> ignore
                Choice1Of2(path)
            else
                Choice2Of2("Could not determine new recording path")
        with
            ex ->   Choice2Of2(ex.NestedMessage)

    member internal r.NotifyOfTermination reason =

        r.OnTermination.Invoke (RecordingTerminatedNotification(r, reason))

and RecordingTerminatedNotification (request : RecordingRequest, reason : RecordingTerminationReason) =

    member __.Reason = reason
    member __.Request = request

and RecordingTerminationHandler = System.Action<RecordingTerminatedNotification>

//-----------------------------------------------------------------------------
// General Processing Commands

type ProcessingCommand =
    | StartRecording of RecordingRequest
    | StopRecording of RecordingRequest
    | StopStartRecording of stop: RecordingRequest * start: RecordingRequest
