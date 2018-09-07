module ScriptBehavior

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Parser.TestInput
open Serilog
open System
open System.Windows.Threading

let mkStandardBehavior () =

    { new IScriptBehavior with
        member __.Focus behaviorContext (focusRange : Parser.FocusRange) =
            let conduit = behaviorContext

            let focusRange = if focusRange = 0.0f then
                                Single.Epsilon // work-around for protobuf
                             else
                                focusRange

            Log.Information("Requesting focus range of {focusRange}", focusRange)
            conduit.RequestFocusDistance(focusRange * 1.0f<m>)

        member __.Wait _behaviorContext (duration : TimeSpan) =
            Log.Information("Waiting for {duration}", duration)
            
            // Don't block the dispatcher.
            let frame = DispatcherFrame()
            Async.Start (async {
                do! Async.Sleep (int duration.TotalMilliseconds)
                frame.Continue <- false
            })
            Dispatcher.PushFrame(frame)
    }
