module BasicConnection

open SoundMetrics.Aris.Comms
open SoundMetrics.Aris.Comms.Internal
open SoundMetrics.Common
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open System
open System.Threading
open TestInputs

let testBasicConnection (inputs : TestInputs) =

    let sn = match inputs.SerialNumber with
             | Some sn -> sn
             | None -> failwith "No serial number was provided"

    // Console doesn't have a sync context by default, we need one for the beacon listener.
    Threading.SynchronizationContext.SetSynchronizationContext(Threading.SynchronizationContext())

    use availability = BeaconListener.CreateForArisExplorerAndVoyager(TimeSpan.FromSeconds(30.0))

    let findTimeout = TimeSpan.FromSeconds(10.0)

    match Async.RunSynchronously(availability.WaitForArisBySerialNumberAsync sn findTimeout) with
    | Some beacon ->
        Log.Information("ARIS {sn}, software version {softwareVersion}, found at {targetIpAddr}",
                        sn, beacon.SoftwareVersion, beacon.IPAddress)

        let initialSettings =
            let defaultSettings = AcousticSettingsRaw.DefaultAcousticSettingsFor beacon.SystemType
            { defaultSettings with FrameRate = 15.0f</s> }

        let perfSink = SampledConduitPerfSink(1000, 10)
        use conduit = new ArisConduit(initialSettings, sn,
                                      FrameStreamReliabilityPolicy.DropPartialFrames,
                                      perfSink)

        use readySignal = new ManualResetEvent(false)

        let mutable frameCount = 0
        let framesExpected = 5
        let mutable errorCount = 0u

        Log.Information("Waiting on a frame...")
        use frames = conduit.Frames.Subscribe(fun readyFrame ->
            let frame = readyFrame.Frame
            Log.Verbose("Received frame {fi} from SN {sn}",
                frame.Header.FrameIndex, frame.Header.SonarSerialNumber)
            frameCount <- frameCount + 1

            if frame.Header.ReorderedSamples = 0u then
                errorCount <- errorCount + 1u
                Log.Error("Frame {fi} is not reordered.", frame.Header.FrameIndex)

            if frameCount >= framesExpected then
                Log.Information("Observed {frameCount} frames, exiting.", frameCount)
                readySignal.Set() |> ignore
        )

        readySignal.WaitOne(findTimeout) |> ignore
        if errorCount = 0u then
            Log.Information("FrameProcessedReport={FrameProcessedReport}",
                            sprintf "%A" perfSink.FrameProcessedReport)
            Ok ()
        else
            Error (sprintf "%u errors occured." errorCount)

    | None -> Error (sprintf "Timed out waiting to find ARIS %d" sn)
