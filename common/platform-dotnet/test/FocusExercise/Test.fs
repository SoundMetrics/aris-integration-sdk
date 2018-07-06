module Test

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.Comms
open SoundMetrics.Aris.Config
open SoundMetrics.Scripting
open System
open System.Reactive.Linq
open System.Threading
open System.Windows.Threading
open System.Threading.Tasks
open System.Reactive
open System.Runtime.CompilerServices

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

open SoundMetrics.Scripting
open SoundMetrics.Scripting.EventMatcher

let isFocusRequest = function | ReceivedFocusCommand _ -> true | _ -> false
let isFocusState = function | UpdatedFocusState _ -> true | _ -> false

let testRawFocusUnits (messageSource : IObservable<SyslogMessage>) =

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
            waitForAsyncWithDispatch
                (detectSequenceAsync messageSource sequence timeout)

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
