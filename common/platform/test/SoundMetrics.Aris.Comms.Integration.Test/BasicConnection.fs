module BasicConnection

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.Comms
open System
open System.Threading
open TestInputs

let testBasicConnection (inputs : TestInputs) =

    let sn = match inputs.SerialNumber with
             | Some sn -> sn
             | None -> failwith "No serial number was provided"

    // Console doesn't have a sync context by default, we need one for the beacon listener.
    let syncContext = Threading.SynchronizationContext()
    use availability =
            BeaconListeners.mkSonarBeaconListener
                NetworkConstants.SonarAvailabilityListenerPortV2
                (TimeSpan.FromSeconds(30.0))
                syncContext
                Beacons.BeaconExpirationPolicy.KeepExpiredBeacons
                None // callbacks

    let timeoutPeriod = TimeSpan.FromSeconds(10.0)

    match FindSonar.findAris availability timeoutPeriod sn with
    | Some beacon ->
        Log.Information("ARIS {sn}, software version {softwareVersion}, found at {targetIpAddr}",
                        sn, beacon.SoftwareVersion, beacon.SrcIpAddr)

        let initialSettings =
            let defaultSettings = AcousticSettings.DefaultAcousticSettingsFor beacon.SystemType
            { defaultSettings with FrameRate = 15.0f</s> }

        let performanceReportSink = SamplePerformanceReportSink(1000, 10)
        use conduit = new SonarConduit(initialSettings, sn, availability,
                                       FrameStreamReliabilityPolicy.DropPartialFrames,
                                       performanceReportSink)

        use readySignal = new ManualResetEvent(false)

        let mutable frameCount = 0
        //let framesExpected = 5
        let endTime = DateTime.Now.Add(timeoutPeriod)
        let mutable errorCount = 0u

        Log.Information("Waiting on a frame...")
        use frames = conduit.Frames.Subscribe(fun processedFrame ->
            match processedFrame.work with
            | Frame (frame, _histogram, _isRecording) ->
                Log.Verbose("Received frame {fi} from SN {sn}",
                    frame.Header.FrameIndex, frame.Header.SonarSerialNumber)
                frameCount <- frameCount + 1

                if frame.Header.ReorderedSamples = 0u then
                    errorCount <- errorCount + 1u
                    Log.Error("Frame {fi} is not reordered.", frame.Header.FrameIndex)

                //if frameCount = framesExpected then
                if DateTime.Now > endTime then
                    readySignal.Set() |> ignore
            | _ -> ()
        )

        readySignal.WaitOne(timeoutPeriod) |> ignore
        if errorCount = 0u then
            Log.Information("FrameProcessedReport={FrameProcessedReport}",
                            sprintf "%A" performanceReportSink.FrameProcessedReport)
            Ok ()
        else
            Error (sprintf "%u errors occured." errorCount)

    | None -> Error (sprintf "Timed out waiting to find ARIS %d" sn)
