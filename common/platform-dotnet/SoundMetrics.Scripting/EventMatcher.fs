namespace SoundMetrics.Scripting

open System
open System.Runtime.CompilerServices

module EventMatcher =
    open Serilog
    open System.Reactive
    open System.Reactive.Linq
    open System.Threading

    // Watches an event stream for a known sequence of events to occur. These sequence
    // is validated by an array of predicates, one per step.
    let detectSequenceAsync<'T> (eventSource : IObservable<'T>)
                                 (sequence : ('T -> bool) array)
                                 (timeout : TimeSpan)
                                 : Async<bool> =
        async {
            let mutable index = 0
            let mutable quitting = false
            use doneSignal = new ManualResetEventSlim(false)

            let quit () =
                quitting <- true
                doneSignal.Set()

            let checkMsg msg =
                if quitting then
                    false // avoid a race condition that handles too many messages
                else
                    let isMatch = sequence.[index]
                    isMatch msg

            let advanceStep () = index <- index + 1
            let isDone () = index = sequence.Length

            use observer =
                new AnonymousObserver<_>(
                    onNext = (fun msg ->
                                if checkMsg msg then
                                    advanceStep()
                                    if isDone() then
                                        Log.Information("waitForSequence: Sequence succeeded")
                                        quit() ),
                    onError = (fun _ ->
                                Log.Error("waitForSequence: Sequence timed out")
                                quit() )
                )

            use _subscription = eventSource.Timeout(timeout).Subscribe(observer)
            let! _ = Async.AwaitWaitHandle(doneSignal.WaitHandle)

            let matchedSteps = index
            let success = matchedSteps = sequence.Length
            return success
        }

    // Watches an event stream for a known sequence of events to occur. These sequence
    // is validated by an array of predicates, one per step.
    let waitForSequence<'T> (eventSource : IObservable<'T>)
                            (steps : ('T -> bool) array)
                            (timeout : TimeSpan)
                            : bool =

        Async.RunSynchronously(detectSequenceAsync eventSource steps timeout)

    let funcsToFuncs<'T> (fs : Func<'T, bool> array) : ('T -> bool) array =
        fs |> Array.map (fun f ->
                            fun (t : 'T) -> f.Invoke(t))

[<Extension>]
module EventMatcherExtensions =
    open EventMatcher
    open System.Threading
    open System.Threading.Tasks

    [<Extension>]
    type IObserver<'T> with
        /// Extension for C# users.
        member __.DetectSequenceAsync (eventSource : IObservable<'T>,
                                       steps : Func<'T, bool> array,
                                       timeout : TimeSpan): Task<bool> =
            Async.StartAsTask(detectSequenceAsync eventSource (steps |> funcsToFuncs) timeout)

        /// Extension for C# users.
        member __.DetectSequenceAsync (eventSource : IObservable<'T>,
                                       steps : Func<'T, bool> array,
                                       timeout : TimeSpan,
                                       cancellationToken : CancellationToken): Task<bool> =
            Async.StartAsTask(detectSequenceAsync eventSource (steps |> funcsToFuncs) timeout,
                                cancellationToken = cancellationToken)
