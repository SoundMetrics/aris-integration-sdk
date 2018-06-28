module Test

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.Comms
open SoundMetrics.Aris.Config
open System
open System.Reactive.Linq
open System.Threading
open System.Windows.Threading
open System.Threading.Tasks
open SyslogReceiver
open System.Reactive

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

let getExplorerBeacon (availables : AvailableSonars) targetSN : SonarBeacon option =

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

//-----------------------------------------------------------------------------

module EventMatcher =

    // Watches an event stream for a known sequence of events to occur. These sequence
    // is validated by an array of predicates, one per step.
    let waitForSequence<'T> (eventSource : IObservable<'T>)
                            (steps : ('T -> bool) array)
                            (timeout : TimeSpan)
                            : bool =

        let mutable index = 0
        let mutable matchedSteps = 0
        let mutable quitting = false
        let frame = DispatcherFrame() // for keeping the dispatcher running

        use observer =
            new AnonymousObserver<_>(
                onNext = (fun msg ->
                            let isMatch = steps.[index]
                            if isMatch msg then
                                matchedSteps <- matchedSteps + 1
                                index <- index + 1
                                if matchedSteps = steps.Length then
                                    Log.Information("waitForSequence: Sequence succeeded")
                                    quitting <- true
                                    frame.Continue <- false),
                onError = (fun _ -> Log.Error("waitForSequence: Sequence timed out")
                                    frame.Continue <- false)
            )

        use _subscription = eventSource.Timeout(timeout).Subscribe(observer)
        Dispatcher.PushFrame(frame)
        let success = matchedSteps = steps.Length
        success

//-----------------------------------------------------------------------------

let isFocusRequest = function | ReceivedFocusCommand _ -> true | _ -> false
let isFocusState = function | UpdatedFocusState _ -> true | _ -> false

open EventMatcher

let testRawFocusUnits (messageSource : IObservable<SyslogReceiver.SyslogMessage>) =

    Log.Debug("testRawFocusUnits")

    SynchronizationContext.SetSynchronizationContext(DispatcherSynchronizationContext())
    assert (SynchronizationContext.Current <> null)

    use availables = BeaconListeners.createDefaultSonarBeaconListener SynchronizationContext.Current

    let targetSN = 24
    let beacon = getExplorerBeacon availables targetSN

    let runTest systemType =
        use conduit = new SonarConduit(
                        AcousticSettings.DefaultAcousticSettingsFor(systemType),
                        targetSN,
                        availables,
                        FrameStreamReliabilityPolicy.DropPartialFrames)

        let runTest setup sequence timeout =
            setup conduit
            waitForSequence messageSource sequence timeout

        runTest (fun conduit -> conduit.RequestFocusDistance(1.0<m>))
                [| isFocusRequest; isFocusState |]
                (TimeSpan.FromSeconds(10.0))
            |> ignore
        runTest (fun conduit -> conduit.RequestFocusDistance(3.0<m>))
                [| isFocusRequest; isFocusState |]
                (TimeSpan.FromSeconds(10.0))
            |> ignore

    match beacon with
    | Some b -> Log.Information("Found SN {targetSN}", targetSN)
                runTest b.SystemType
    | None -> Log.Error("Couldn't find a beacon for SN {targetSN}", targetSN)

    Log.Information("testRawFocusUnits completed.")
