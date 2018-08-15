module RecordingStats

open SoundMetrics.Aris.Comms

open SoundMetrics.Aris.Comms.Internal
open SoundMetrics.Common
open FrameProcessing
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open System
open System.Threading
open TestInputs

let recordingStats (inputs : TestInputs) =

    let sn = match inputs.SerialNumber with
             | Some sn -> sn
             | None -> failwith "No serial number was provided"

    // Console doesn't have a sync context by default, we need one for the beacon listener.
    Threading.SynchronizationContext.SetSynchronizationContext(Threading.SynchronizationContext())

    use availability =
            BeaconListener.CreateForArisExplorerAndVoyager(TimeSpan.FromSeconds(30.0))
    let timeoutPeriod = TimeSpan.FromSeconds(10.0)

    match Async.RunSynchronously(availability.WaitForArisBySerialNumberAsync sn timeoutPeriod) with
    | Some beacon ->
        Log.Information("ARIS {sn}, software version {softwareVersion}, found at {targetIpAddr}",
                        sn, beacon.SoftwareVersion, beacon.IPAddress)

        let initialSettings =
            let defaultSettings = AcousticSettings.DefaultAcousticSettingsFor beacon.SystemType
            { defaultSettings with FrameRate = 15.0f</s> }

        let skipFrames = 10
        let sampleCountWanted = 500
        let perfSink = SampledConduitPerfSink(sampleCountWanted, skipFrames)
        use conduit = new ArisConduit(initialSettings, sn,
                                      FrameStreamReliabilityPolicy.DropPartialFrames,
                                      perfSink)

        use readySignal = new ManualResetEvent(false)

        let mutable frameCount = 0
        let mutable errorCount = 0u

        let recordingFile =
            let path = IO.Path.GetTempFileName()
            IO.File.Delete(path)
            path

        let recordingRequest =
            let getPath _ _ = struct (true, recordingFile)
            let onTermination (notification : RecordingTerminatedNotification) =
                Log.Error("Recording was terminated: {reason}", notification.Reason)
            RecordingRequest.Create("RecordingStats",
                                    RecordingPathFactory(getPath),
                                    RecordingTerminationHandler(onTermination))

        if conduit.StartRecording(recordingRequest) then
            Log.Information("Recording to {recordingFile}", recordingFile)
        else
            Log.Error("Could not start a recording.")

        Log.Information("Waiting on a frame...")
        use frames = conduit.Frames.Subscribe(fun readyFrame ->
            let frame = readyFrame.Frame
            Log.Verbose("Received frame {fi} from SN {sn}",
                frame.Header.FrameIndex, frame.Header.SonarSerialNumber)
            frameCount <- frameCount + 1

            if frame.Header.ReorderedSamples = 0u then
                errorCount <- errorCount + 1u
                Log.Error("Frame {fi} is not reordered.", frame.Header.FrameIndex)

            if perfSink.IsFull then
                Log.Information("Exiting, {frameCount} frames collected",
                                perfSink.SamplesCollected)
                readySignal.Set() |> ignore
        )

        readySignal.WaitOne(-1) |> ignore
        conduit.StopRecording(recordingRequest) |> ignore
        if errorCount = 0u then
            Log.Information("States follow; durations in \u00B5s unless otherwise indicated")
            Log.Information("FrameProcessedReport={FrameProcessedReport}",
                            sprintf "%A" perfSink.FrameProcessedReport)
            Log.Information("FrameReorderedReport={FrameReorderedReport}",
                            sprintf "%A" perfSink.FrameReorderedReport)
            Log.Information("FrameRecordedReport={FrameRecordedReport}",
                            sprintf "%A" perfSink.FrameRecordedReport)
            Ok ()
        else
            Error (sprintf "%u errors occured." errorCount)

    | None -> Error (sprintf "Timed out waiting to find ARIS %d" sn)
