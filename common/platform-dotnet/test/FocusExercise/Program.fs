module Main

open Serilog
open SoundMetrics.Scripting
open System
open System.Reactive.Linq
open System.Threading
open System.Threading.Tasks
open System.Windows.Threading

[<Literal>]
let LoggingTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"

[<EntryPoint>]
let main _argv =

    Thread.CurrentThread.Name <- "Main thread"

    Log.Logger <- (new LoggerConfiguration())
                    //.MinimumLevel.Debug()
                    .WriteTo.Console(outputTemplate = LoggingTemplate)
                    .CreateLogger()

    //SyslogReceiver.test () |> ignore

    SynchronizationContext.SetSynchronizationContext(DispatcherSynchronizationContext())

    use syslogListener = new SyslogListener()

    let frame = DispatcherFrame()
    ThreadPool.QueueUserWorkItem(fun _ ->   Thread.CurrentThread.Name <- "Test.testRawFocusUnits"
                                            Test.testRawFocusUnits syslogListener.Messages
                                            frame.Continue <- false) |> ignore
    Dispatcher.PushFrame(frame)

    0
