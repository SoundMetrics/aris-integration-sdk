module Test

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.Comms
open SoundMetrics.Aris.Config
open System
open System.Threading
open System.Windows.Threading
open System.Threading.Tasks

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

let getExplorerBeacon (availables : AvailableSonars) targetSN =

    let timeout = TimeSpan.FromSeconds(4.0)

#if USE_THE_TASK_BASED_FUNCTIONS
    let beacon =
        let beaconTask =
            availables.WaitForExplorerBySerialNumberAsync(timeout, targetSN)

        let frame = DispatcherFrame()
        ThreadPool.QueueUserWorkItem(fun _ ->   try
                                                    beaconTask.Wait()
                                                with
                                                    :? TaskCanceledException -> ()
                                                frame.Continue <- false)
            |> ignore
        Dispatcher.PushFrame(frame)
        beaconTask.Result

    if Object.ReferenceEquals(beacon, null) then
        None
    else
        Some beacon
#else
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
#endif


let testRawFocusUnits (messageSource : IObservable<SyslogReceiver.SyslogMessage>) =

    Log.Debug("testRawFocusUnits")

    SynchronizationContext.SetSynchronizationContext(DispatcherSynchronizationContext())
    assert (SynchronizationContext.Current <> null)

    use availables = BeaconListeners.createDefaultSonarBeaconListener SynchronizationContext.Current

    let targetSN = 24
    let beacon = getExplorerBeacon availables targetSN

    let runTest systemType =
        use conduit = new SonarConduit(
                        AcousticSettings.DefaultAcousticSettingsFor(systemType), // TODO 
                        targetSN,
                        availables,
                        FrameStreamReliabilityPolicy.DropPartialFrames)

        let syslogSub = messageSource.Subscribe(fun ev -> printfn "ev=%A" ev)

        let frame = DispatcherFrame()
        Async.Start(async {
            do! Async.Sleep(1000)
            for i = 1 to 2 do
                conduit.RequestFocusDistance(1.0<m>)
                do! Async.Sleep(5000)

                conduit.RequestFocusDistance(double i * 1.0<m>)
                do! Async.Sleep(5000)
            frame.Continue <- false
        })
        Dispatcher.PushFrame(frame)
        syslogSub.Dispose()

    match beacon with
    | Some b -> Log.Information("Found SN {targetSN}", targetSN)
                runTest b.SystemType
    | None -> Log.Error("Couldn't find a beacon for SN {targetSN}", targetSN)

    Log.Information("testRawFocusUnits completed.")
