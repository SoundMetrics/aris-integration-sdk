module ArisSupport

open SoundMetrics.Common
open SoundMetrics.Common.ArisBeaconDetails
open System
open System.Threading
open System.Windows.Threading

//-----------------------------------------------------------------------------
// Synchronization Context
//
// In a console application (like this) the sync context can be problematic.
// GUI apps are much easier this way.
// This code uses the Dispatcher.PushFrame technique available in the .NET
// Framework (Desktop).
//-----------------------------------------------------------------------------

let getArisBeacon (availables : BeaconListener) targetSN : ArisBeacon option =

    assert (not (isNull SynchronizationContext.Current))

    let timeout = TimeSpan.FromSeconds(4.0)

    let beacon =
        let mutable someBeacon = None
        let frame = DispatcherFrame()
        Async.Start (async {
            try
                let! b = availables.WaitForArisBySerialNumberAsync targetSN timeout
                someBeacon <- b
            finally
                frame.Continue <- false
        })
        Dispatcher.PushFrame(frame)
        someBeacon
    beacon
