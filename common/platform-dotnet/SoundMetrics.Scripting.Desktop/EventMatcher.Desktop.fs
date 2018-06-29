namespace SoundMetrics.Scripting.Desktop

open SoundMetrics.Scripting.EventMatcher
open System
open System.Runtime.CompilerServices

module EventMatcher =
    open System.Windows.Threading

    // Watches an event stream for a known sequence of events to occur. These sequence
    // is validated by an array of predicates, one per step.
    let waitForSequenceWithDispatch<'T> (eventSource : IObservable<'T>)
                                        (steps : ('T -> bool) array)
                                        (timeout : TimeSpan)
                                        : bool =
        let mutable result = false
        let frame = DispatcherFrame()
        Async.Start(async {
            let! r = detectSequenceAsync eventSource steps timeout
            result <- r
            frame.Continue <- false
        })

        Dispatcher.PushFrame(frame)
        result

[<Extension>]
module EventMatcherExtensions =
    open EventMatcher

    [<Extension>]
    type IObserver<'T> with
        member __.WaitForSequenceWithDispatch (eventSource : IObservable<'T>,
                                               steps : Func<'T, bool> array,
                                               timeout : TimeSpan)
                                               : bool =
            waitForSequenceWithDispatch eventSource (steps |> funcsToFuncs) timeout
