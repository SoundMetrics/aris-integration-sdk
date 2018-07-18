module Test

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.Comms
open SoundMetrics.Aris.Comms.Internal
open SoundMetrics.Scripting
open System
open System.Threading
open System.Windows.Threading

type FU = int
type AvailableSonars = Beacons.BeaconSource<SonarBeacon, SerialNumber>


//-----------------------------------------------------------------------------
// Synchronization Context
//
// In a console application (like this) the sync context can be problematic.
// GUI apps are much easier this way.
// This code uses the Dispatcher.PushFrame technique available in the .NET
// Framework (Desktop).
//-----------------------------------------------------------------------------

let getArisBeacon (availables : AvailableSonars) targetSN : SonarBeacon option =

    let timeout = TimeSpan.FromSeconds(4.0)

    let beacon =
        let mutable someBeacon = None
        let frame = DispatcherFrame()
        Async.Start (async {
            try
                let! b = waitForExplorerBySerialNumberAsync availables timeout targetSN
                someBeacon <- b
            finally
                frame.Continue <- false
        })
        Dispatcher.PushFrame(frame)
        someBeacon
    beacon


//-----------------------------------------------------------------------------

open SoundMetrics.Scripting.EventMatcher


let runTest eventSource (series : SetupAndMatch<SyslogMessage, unit> array) timeout =

    waitForAsyncWithDispatch
        (runSetupMatchValidateAsync eventSource () series timeout)

let getAFrame (conduit : ArisConduit) (timeout : TimeSpan) =

    let mutable frame = None
    waitForAsyncWithDispatch (async {
        use doneSignal = new ManualResetEventSlim()
        use _sub = conduit.Frames.Subscribe(fun f ->
                        frame <- Some f
                        doneSignal.Set())
        doneSignal.Wait(timeout) |> ignore
        return true
    }) |> ignore

    frame


let exit code = Environment.Exit(code)

let isFocusRequest = Func<SyslogMessage,bool>(function | ReceivedFocusCommand _ -> true | _ -> false)
let isFocusState = Func<SyslogMessage,bool>(function | UpdatedFocusState _ -> true | _ -> false)

let testRawFocusUnits (eventSource : IObservable<SyslogMessage>) =

    Log.Debug("testRawFocusUnits")

    SynchronizationContext.SetSynchronizationContext(DispatcherSynchronizationContext())
    assert (SynchronizationContext.Current <> null)

    use availables = BeaconListeners.createDefaultSonarBeaconListener SynchronizationContext.Current

    let targetSN = 24
    let beacon = getArisBeacon availables targetSN

    let runTest (beacon : SonarBeacon) =
        Log.Information("Running test...")
        Log.Information("Traget sonar beacon={beacon}", beacon)
        use conduit = new ArisConduit(
                        AcousticSettings.DefaultAcousticSettingsFor(beacon.SystemType),
                        targetSN,
                        availables,
                        FrameStreamReliabilityPolicy.DropPartialFrames)

        // wait for connection
        let connected =
            waitForTaskWithDispatch (conduit.WaitForConnectionAsync(TimeSpan.FromSeconds(5.0)))
        if connected then
            Log.Information("Sonar connected")
        else
            Log.Error("Timed out waiting for sonar connection")
            exit 2

        let showUnits range () = // add unit for use as partial application below
            match getAFrame conduit (TimeSpan.FromSeconds(2.0)) with
            | Some readyFrame ->
                let frame = readyFrame.Frame
                Log.Information("Frame info: fu={fu}; focus range={focusRange}",
                                            frame.Header.Focus,
                                            range)
            | None -> Log.Error("Attempt to get a frame failed")

        let range1 = 1.0<m>
        let range2 = 3.0<m>

        let series = [|
            {
                SetupAndMatch.Description = "My first step"
                SetUp = fun _ ->    conduit.SetSalinity(Salinity.Brackish)
                                    conduit.RequestFocusDistance(range1 + 1.0<m>)
                                    conduit.RequestFocusDistance(range1)
                                    true
                Expecteds =
                    [|
                        { Description = "Found focus request"; Match = isFocusRequest }
                        { Description = "Observed focus state"; Match = isFocusState }
                    |]
                OnSuccess = showUnits range1
            }
            {
                SetupAndMatch.Description = "My second step"
                SetUp = fun _ ->    conduit.SetSalinity(Salinity.Brackish)
                                    conduit.RequestFocusDistance(range2)
                                    true
                Expecteds =
                    [|
                        { Description = "Found focus request"; Match = isFocusRequest }
                        { Description = "Observed focus state"; Match = isFocusState }
                    |]
                OnSuccess = showUnits range2
            }
        |]

        if not (runTest eventSource series (TimeSpan.FromSeconds(10.0))) then
            Log.Error("Test failed")
            exit 1
        else
            Log.Information("Test succeeded")

    match beacon with
    | Some bcn -> Log.Information("Found SN {targetSN}", targetSN)
                  runTest bcn
    | None -> Log.Error("Couldn't find a beacon for SN {targetSN}", targetSN)

    Log.Information("testRawFocusUnits completed.")
