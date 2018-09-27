// Copyright (c) 2015-2018 Sound Metrics. All Rights Reserved. 

namespace SoundMetrics.Common

open System.Collections.Concurrent
open System.Threading

/// A queued (ordered) synchronization context for use in services
/// or command-line applications.
/// Based on the implementation found here:
/// http://blogs.msdn.com/b/pfxteam/archive/2012/01/20/10259049.aspx
[<Sealed>]
type QueuedSynchronizationContext(ct : CancellationToken) =

    inherit SynchronizationContext()

    let isDisposed = ref false
    let workQueue = new BlockingCollection<_>()
    let doneSignal = new ManualResetEventSlim()


    interface System.IDisposable with
        member c.Dispose() =
            if not workQueue.IsCompleted then
                c.Complete()

            doneSignal.Wait()
            workQueue.Dispose()
            doneSignal.Dispose()

    member c.Dispose () = (c :> System.IDisposable).Dispose()


    override __.Post(d : SendOrPostCallback, state : obj) =

        if d = Unchecked.defaultof<_> then
            invalidArg "d" "Value is null"

        let workItem = d, state
        workQueue.Add(workItem)


    override __.Send(_d : SendOrPostCallback, _state : obj) =

        failwith "Send is not supported"


    member __.RunOnCurrentThread() =

        try
            try
                for (d, state) in workQueue.GetConsumingEnumerable(ct) do
                    d.Invoke(state)
            with
                | :? System.OperationCanceledException -> ()
        finally
            doneSignal.Set()


    /// Notifies the context that no more work will arrive.
    member __.Complete() = workQueue.CompleteAdding()


    static member RunOnAThread (ct : CancellationToken) =

        let context = new QueuedSynchronizationContext(ct)

        let threadProc () = context.RunOnCurrentThread()

        let thread = Thread(threadProc)

        thread.Start()
        context
