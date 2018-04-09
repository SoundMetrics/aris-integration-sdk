module BasicConnection

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

    let timeoutPeriod = TimeSpan.FromSeconds(5.0)

    match FindSonar.findAris availability timeoutPeriod sn with
    | Some beacon ->
        Log.Information("ARIS {sn}, software version {softwareVersion}, found at {targetIpAddr}",
                        sn, beacon.SoftwareVersion, beacon.SrcIpAddr)

        let initialSettings = AcousticSettings.DefaultAcousticSettingsFor beacon.SystemType
        use conduit = new SonarConduit(initialSettings, sn, availability, FrameStreamReliabilityPolicy.DropPartialFrames)

        use readySignal = new ManualResetEvent(false)

        Log.Information("Waiting on a frame...")
        use frames = conduit.Frames.Subscribe(fun processedFrame ->
            readySignal.Set() |> ignore
            match processedFrame.work with
            | Frame (frame, _histogram, _isRecording) ->
                Log.Information("Received frame {fi} from SN {sn}",
                    frame.Header.FrameIndex, frame.Header.SonarSerialNumber)
            | _ -> ()
        )

        if readySignal.WaitOne(TimeSpan.FromSeconds(15.0)) then
            Ok ()
        else
            Error "Timed out waiting for a frame"
    | None -> Error (sprintf "Timed out waiting to find ARIS %d" sn)
