namespace SoundMetrics.Scripting

open System

[<AutoOpen>]
module DispatcherHelperFSharp =
    open System.Runtime.CompilerServices
    open System.Windows.Threading

    /// Pushes a dispatcher frame and waits for the function to run on an asynchronous context.
    let waitForFuncWithDispatch (workerFunction : unit -> bool) : bool =

        let mutable result = false
        let frame = DispatcherFrame()
        Async.Start(async {
            result <- workerFunction()
            frame.Continue <- false
        })

        Dispatcher.PushFrame(frame)
        result

    /// Pushes a dispatcher frame and waits for the Async to run on an asynchronous context.
    let waitForAsyncWithDispatch (work : Async<bool>) : bool =

        waitForFuncWithDispatch (fun () -> Async.RunSynchronously work)

module DispatcherHelper =
    open System.Runtime.CompilerServices
    /// Pushes a dispatcher frame and waits for the function to run on an asynchronous context.
    [<CompiledName("WaitForFuncWithDispatch")>]
    let waitForFuncWithDispatchCSharp (workerFunction : Func<bool>) : bool =

        waitForFuncWithDispatch (fun () -> workerFunction.Invoke())
