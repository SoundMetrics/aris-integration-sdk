// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open FrameProcessing
open RecordingLog
open System

module internal Recording =

    type RecordingRequestInfo = {
        Request: RecordingRequest
        RecordingPath: RecordingPath
        Recorder: FileRecording.FixedFrameSizeRecorder
    }
    with
        member ri.Dispose() = ri.Recorder.Dispose()

    type FrameSize = { BeamCount: uint32; SampleCount: uint32 }
    with
        member s.Equals (beamCount, sampleCount) =
            s.BeamCount = beamCount && s.SampleCount = sampleCount

    type RecordingState = {
        ReqInfos: RecordingRequestInfo list ref
        QueuedStartRecordRequest: RecordingRequest option ref
        FrameSize: FrameSize ref
    }
    with
        static member Create () = {
            ReqInfos = ref []
            QueuedStartRecordRequest = ref None
            FrameSize = ref { BeamCount = 0u; SampleCount = 0u }
        }

    module private RecordingImpl =
        open SoundMetrics.Aris.Comms.PerformanceTiming

        let noRecordingFailure = { FailedPath = None }

        type ProcessFrameError = string

        type FrameErrors (errors : string list) =

            new() = FrameErrors([])
            new(error : string) = FrameErrors([error])

            member __.Append error = FrameErrors(error :: errors)
            member __.All = errors |> List.rev
            member __.IsEmpty = errors.IsEmpty

        // Note: the error list is in reverse chronological order
        type ReqInProgress = RecordingRequestInfo * FrameErrors

        let clearErrors (rip : ReqInProgress) = (fst rip), FrameErrors()
        let appendError error (rip : ReqInProgress) = (fst rip), (snd rip).Append(error)

        let mapTuple mapP mapQ (ps, qs) = ps |> List.map mapP, qs |> List.map mapQ

        let findMatchingRequestInfos request reqInfos =
            // Return (matches, remainder).
            reqInfos |> List.partition (fun ri -> System.Object.ReferenceEquals(request, ri.Request))


        /// Starts a recording.
        let startRecording request (beamCount : uint32) (sampleCount : uint32) prevRecordingFailure =

            let timestamp = match request.JobDisplayTime with
                            | Some ts -> ts
                            | None -> DateTime.Now

            match request.BuildRecordingPath timestamp beamCount sampleCount prevRecordingFailure with
            | Choice1Of2 newPath ->
                try
                    logStartedRecording request.Description newPath
                    Choice1Of2({ Request = request
                                 RecordingPath = newPath
                                 Recorder = new FileRecording.FixedFrameSizeRecorder(newPath) })
                with
                | :? System.IO.IOException as ex ->
                    let msg = sprintf "Couldn't start a new recording at '%s':\n%s" newPath ex.Message
                    Choice2Of2(msg)
            | Choice2Of2 error ->
                logNoAvailableRecordingPath request.Description
                Choice2Of2(sprintf "No available recording path for '%s'; %s" request.Description error)


        /// Starts the recording and returns the new list of recording requests
        /// paired with an optional failed request and the failure reason
        let startRecordingNoDupes reqInfos request beamCount sampleCount prevRecordingFailure =

            let matches, _ = reqInfos |> findMatchingRequestInfos request
            match matches with
            | [] ->
                // There's no existing request.
                match startRecording request beamCount sampleCount prevRecordingFailure with
                | Choice1Of2 req -> req :: reqInfos, None
                | Choice2Of2 error -> reqInfos, Some error
            | _  ->
                // There's an existing request that matches this one.
                logDuplicateRecordingRequest request.Description
                reqInfos, None

        /// Stops the recording and returns the new list of recording requests.
        let stopRecording reqInfos request =

            let matches, remainder = reqInfos |> findMatchingRequestInfos request
            if matches.Length = 0 then
                logStopRequestNotFound request.Description
            else
                matches |> List.iter (fun req -> req.Recorder.Dispose ())
                logStoppedRecording request.Description matches.Head.RecordingPath
            remainder

        /// Stops all recordings and returns the new (empty) list of recording requests.
        let stopAll reqInfos =
            let rec stopRecordings reqInfos =
                match reqInfos with
                | [] -> []
                | head :: _ ->
                    let remainder = stopRecording reqInfos head.Request
                    stopRecordings remainder

            let newList = stopRecordings reqInfos
            logStoppedAllRecordings ()
            newList

        // Side effects on reqInfo
        let processFrameForRequest (reqInProgress : ReqInProgress) frame recordFrameIndexOffset
                : bool * ReqInProgress =

            try
                let reqInfo = fst reqInProgress
                let frameIndexOffset = reqInfo.Recorder.WriteFrame (frame)
                recordFrameIndexOffset reqInfo.Request frameIndexOffset
                true, reqInProgress // Assumes mutation to (fst reqInProgress)
            with
            | :? System.IO.IOException as ex ->
                let path = (fst reqInProgress).RecordingPath
                let msg = sprintf "Error when recording frame to '%s':\n%s" path ex.Message
                false, (reqInProgress |> appendError msg)

        /// Forwards a frame to recording requests for recording. Returns a tuple of lists, the
        /// first of which were successful, the second failed. The list elements are a tuple of
        /// request and error message (when failed).
        let processFrameOverRequests (reqInfos : ReqInProgress list) frame recordFrameIndexOffset
                : ReqInProgress list * ReqInProgress list =

            // Build a list of requests paired with optional error message
            let goods, bads = reqInfos |> List.map (fun r -> processFrameForRequest r frame recordFrameIndexOffset)
                                       |> List.partition (fun (success, _) -> success)

            // Drop the success flag.
            goods |> List.map snd, bads |> List.map snd


        // Side effects on recordingState.reqInfos
        let processInput (perfSink : ConduitPerfSink) recordFrameIndexOffset recordingState workUnit =

            let struct (_result, sw) = timeThis (fun _sw ->
                let reqInfos = recordingState.ReqInfos
                let startRecordRequest = recordingState.QueuedStartRecordRequest
                let frameSize = recordingState.FrameSize

                // We wait until the next frame to actually start a recording so we can
                // pass beam/sample count to the client for naming.
                let queueRecordingRequest req =
                    startRecordRequest := Some req
                    sprintf "queued recording request: '%s'" req.Description

                match workUnit.Work with
                | Frame readyFrame ->
                    let frame = readyFrame.Frame
                    // Check whether the frame size has changed; if so, stop and restart current recordings.
                    let beamCount = frame.BeamCount
                    let sampleCount = frame.Header.SamplesPerBeam

                    // Address a new frame size arriving--currently we cannot record more than one frame
                    // size to a .aris file.
                    let badResizeRestarts =
                        let sizeChanged = not ((!frameSize).Equals(beamCount, sampleCount))
                        if sizeChanged then
                            frameSize := { BeamCount = beamCount; SampleCount = sampleCount }
                            let currents = !reqInfos
                            if currents.IsEmpty then
                                []
                            else
                                reqInfos := stopAll currents
                                let results = seq {
                                    for req in currents do
                                        yield req, startRecording req.Request beamCount sampleCount RecordingFailure.None }

                                let restarted, badRestarts =
                                        results |> Seq.map (fun (ri, ch) ->
                                                                match ch with
                                                                | Choice1Of2 newRi -> newRi, FrameErrors()
                                                                | Choice2Of2 (error) -> ri, FrameErrors(error))
                                                |> Seq.toList
                                                |> List.partition (fun rip -> (snd rip).IsEmpty)
                                                |> mapTuple fst id

                                reqInfos := restarted
                                badRestarts
                        else
                            []

                    // Check for a queued recording request
                    let recActionMsg =
                        match !startRecordRequest with
                        | Some req ->
                            let ris, error = startRecordingNoDupes !reqInfos req frame.BeamCount frame.Header.SamplesPerBeam
                                                    RecordingFailure.None
                            match error with
                            | Some error -> req.NotifyOfTermination(RecordingError([error]))
                            | None -> reqInfos := ris

                            startRecordRequest := None
                            "started recording; "
                        | None -> ""

                    // Process the frame with some number of retries for write failures.
                    let rec attemptFrameAndUpdateReqs (reqsTodo : ReqInProgress list)
                                                        frame
                                                        recordFrameIndexOffset
                            : ReqInProgress list * ReqInProgress list =

                        if reqsTodo.IsEmpty then
                            [], []
                        else
                            // Failed here refers to recording requests that have not written successfully.
                            let complete, failed = processFrameOverRequests reqsTodo frame recordFrameIndexOffset

                            let rec restartFailedReqs retryReqs recoveredReqs gaveUpReqs =

                                let maxRecordRetries = 3

                                match retryReqs with
                                | [] -> recoveredReqs, gaveUpReqs
                                | head :: tail ->
                                    let rec retry (rip : ReqInProgress) lastBadPath attemptsRemaining
                                            : Choice<ReqInProgress, ReqInProgress> =

                                        if attemptsRemaining > 0 then
                                            let ri = fst rip
                                            let req = ri.Request
                                            match req.BuildRecordingPath DateTime.Now
                                                                            frame.BeamCount
                                                                            frame.Header.SamplesPerBeam
                                                                            { FailedPath = Some ri.RecordingPath } with
                                            | Choice1Of2 newPath ->
                                                let recFail = { FailedPath = Some lastBadPath }
                                                match startRecording req frame.BeamCount frame.Header.SamplesPerBeam recFail with
                                                | Choice1Of2 ri ->
                                                    let reqInProgress = ri, snd rip
                                                    match processFrameForRequest reqInProgress frame recordFrameIndexOffset with
                                                    | true, rip -> Choice1Of2 (rip |> clearErrors)
                                                    | false, rip -> retry rip newPath (attemptsRemaining - 1)

                                                | Choice2Of2 error ->
                                                    retry (rip |> appendError error) newPath (attemptsRemaining - 1)

                                            | Choice2Of2 error ->
                                                retry (rip |> appendError error) lastBadPath (attemptsRemaining - 1)
                                        else
                                            Choice2Of2 rip

                                    match retry head (fst head).RecordingPath maxRecordRetries with
                                    | Choice1Of2 rip -> restartFailedReqs tail (rip :: recoveredReqs) gaveUpReqs
                                    | Choice2Of2 rip -> restartFailedReqs tail recoveredReqs (rip :: gaveUpReqs)


                            let recovered, gaveUp = restartFailedReqs failed [] []

                            complete @ recovered, gaveUp

                    // Process the frame
                    let goods, bads = attemptFrameAndUpdateReqs (!reqInfos |> List.map (fun r -> r, FrameErrors()))
                                                                frame
                                                                recordFrameIndexOffset
                    reqInfos := goods |> List.map fst
                    for (ri, errors) in badResizeRestarts @ bads do
                        ri.Request.NotifyOfTermination(RecordingError(errors.All))
                        ri.Dispose()

                    recActionMsg + "record frame"

                | Command cmd ->
                    match cmd with
                    | StartRecording req -> queueRecordingRequest req
                    | StopRecording req ->  reqInfos := stopRecording !reqInfos req
                                            "stop recording"
                    | StopStartRecording (stopReq, startReq) ->
                        reqInfos := stopRecording !reqInfos stopReq
                        "stopped recording; " + queueRecordingRequest startReq
                | Quit ->
                    reqInfos := stopAll !reqInfos
                    "quit"
            )

            perfSink.FrameRecorded sw


    let recordFrame perfSink mapFrameIndex recordingState input =

        RecordingImpl.processInput perfSink mapFrameIndex recordingState input
