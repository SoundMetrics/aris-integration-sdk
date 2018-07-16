namespace SoundMetrics.Scripting

open System
open System.Runtime.CompilerServices

module EventMatcher =
    open Serilog
    open System.Reactive
    open System.Reactive.Linq
    open System.Threading

    type ExpectedEvent<'Ev> = {
        Description     : string
        Match           : Func<'Ev, bool> // Func<,> for interop
    }

    // Watches an event stream for a known sequence of events to occur. These sequence
    // is validated by an array of predicates, one per step.
    let detectSequenceAsync (eventSource : IObservable<'Ev>)
                            (series : ExpectedEvent<'Ev> array)
                            (timeout : TimeSpan)
                            : Async<bool> =
        async {
            let mutable index = 0
            let mutable quitting = false
            use doneSignal = new ManualResetEventSlim(false)

            let quit () =
                quitting <- true
                doneSignal.Set()

            let checkEvent ev =
                if quitting then
                    false // avoid the race condition that handles extra events
                else
                    let isMatch = series.[index].Match
                    isMatch.Invoke(ev)

            let advanceStep () = index <- index + 1
            let isDone () = index = series.Length

            use observer =
                new AnonymousObserver<_>(
                    onNext = (fun ev ->
                                if checkEvent ev then
                                    advanceStep()
                                    if isDone() then
                                        Log.Information("detectSequenceAsync: Sequence succeeded")
                                        quit() ),
                    onError = (fun _ ->
                                Log.Error("detectSequenceAsync: Sequence timed out")
                                quit() )
                )

            use _subscription = eventSource.Timeout(timeout).Subscribe(observer)
            let! _ = Async.AwaitWaitHandle(doneSignal.WaitHandle)

            let matchedSteps = index
            let success = matchedSteps = series.Length
            return success
        }

    // Watches an event stream for a known sequence of events to occur. These sequence
    // is validated by an array of predicates, one per step.
    let waitForSequence (eventSource : IObservable<'Ev>)
                        (series : ExpectedEvent<'Ev> array)
                        (timeout : TimeSpan)
                        : bool =

        Async.RunSynchronously(detectSequenceAsync eventSource series timeout)

    type SetupAndMatch<'Ev, 'St> = {
        Description     : string
        SetUp           : 'St -> bool
        Expecteds       : ExpectedEvent<'Ev> array
    }

    let runSetupMatchValidateAsync (eventSource : IObservable<'Ev>)
                                   (state : 'St)
                                   (series : SetupAndMatch<'Ev, 'St> array)
                                   (timeout : TimeSpan)
                                   : Async<bool> =

        Log.Information("runSetupMatchValidateAsync: checking input")

        if series.Length = 0 then
            invalidArg "series" "An empty series is not allowed"

        if series |> Seq.map (fun step -> step.Expecteds)
                  |> Seq.exists (fun expecteds -> expecteds.Length = 0) then
            invalidArg "series.Expecteds" "Empty expecteds are not allowed"

        async {
            let mutable success = true
            let mutable stepNum = 1

            for step in series do
                if success then
                    Log.Information("Starting step {stepNum}: {stepDescription}", stepNum, step.Description)
                    success <- step.SetUp state
                    if success then
                        let! result =  detectSequenceAsync eventSource step.Expecteds timeout
                        success <- result
                else
                    Log.Warning("Skipping step {stepNum}: {stepDescription}", stepNum, step.Description)

                stepNum <- stepNum + 1

            return success
        }


[<Extension>]
module EventMatcherExtensions =
    open EventMatcher
    open System.Threading
    open System.Threading.Tasks

    [<Extension>]
    type IObserver<'Ev> with

        /// Extension for C# users.
        member __.DetectSequenceAsync (eventSource : IObservable<'Ev>,
                                       series : ExpectedEvent<'Ev> array,
                                       timeout : TimeSpan,
                                       cancellationToken : CancellationToken): Task<bool> =

            if isNull eventSource then invalidArg "eventSource" "Cannot be null"
            if isNull series then invalidArg "steps" "Cannot be null"

            Async.StartAsTask(detectSequenceAsync eventSource series timeout,
                              cancellationToken = cancellationToken)

        /// Extension for C# users.
        member ob.DetectSequenceAsync (eventSource : IObservable<'Ev>,
                                       series : ExpectedEvent<'Ev> array,
                                       timeout : TimeSpan): Task<bool> =

            ob.DetectSequenceAsync(eventSource, series, timeout, CancellationToken.None)
