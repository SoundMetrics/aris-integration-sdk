// Copyright 2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

// This functionality is meant to simplify tracking asynchronous tasks
// and allowing cancellation and clean-up on shutting an object instance down.


open Serilog
open System
open System.Threading
open System.Threading.Tasks

module internal AsyncTaskTracker =

    let inline addTask<'T> (fn : Func<'T>)
                           (taskCount : int ref)
                           (ct : CancellationToken)
                           signalEmpty
            : Task<'T> =

        let fn' = Func<'T>(fun () ->
            try
                try
                    Log.Debug("*before*")
                    let t = fn.Invoke()
                    Log.Debug("*after*")
                    t
                finally
                    Log.Debug("*finally*")
                    if Interlocked.Decrement(taskCount) = 0 then
                        signalEmpty()
            with
                ex -> Log.Warning("AsyncTaskTracker: a task threw an exception: {ex}", ex)
                      reraise()
        )

        Interlocked.Increment(taskCount) |> ignore
        Task.Factory.StartNew(fn', ct)


open AsyncTaskTracker

type internal AsyncTaskTracker (disposeTimeout : TimeSpan) =

    let mutable disposed = false
    let taskCount = ref 0
    let cts = new CancellationTokenSource()
    let emptySignal = new ManualResetEventSlim()

    let signalEmpty () = emptySignal.Set()

    let waitForCompletion (timeout : TimeSpan) = 
        if !taskCount = 0 then
            true
        else
            emptySignal.Wait(timeout)

    let cleanUp disposing =
        if disposing then
            // Clean up managed resources
            if disposed then raise (ObjectDisposedException("AsyncTaskTracker is disposed"))

            if !taskCount > 0 then
                Log.Debug("cleanUp taskCount > zero")
                cts.Cancel()
                if not (waitForCompletion disposeTimeout) then
                    Log.Warning("AsyncTaskTracker: not all tasks completed before disposal")
            else
                Log.Debug("cleanUp taskCount = zero")

            cts.Dispose()
            emptySignal.Dispose()

        // Clean up unmanaged resources.
        ()

    interface IDisposable with
        member s.Dispose() = cleanUp true
                             GC.SuppressFinalize(s)

    override __.Finalize() = cleanUp false

    member __.AddTask(fn) : Task<'T> =

        emptySignal.Reset()
        addTask fn taskCount cts.Token signalEmpty

    member __.WaitForCompletionAsync(timeout : TimeSpan) : Task<bool> =

        Task.Factory.StartNew(Func<bool>(fun () -> waitForCompletion timeout))
