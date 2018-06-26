module Main

open Serilog
open System
open System.Reactive.Linq
open System.Threading
open System.Threading.Tasks
open System.Windows.Threading

[<Literal>]
let LoggingTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"

[<EntryPoint>]
let main argv =

    Log.Logger <- (new LoggerConfiguration())
                    .MinimumLevel.Debug()
                    .WriteTo.Console(outputTemplate = LoggingTemplate)
                    .CreateLogger()

    //SyslogReceiver.test () |> ignore

    SynchronizationContext.SetSynchronizationContext(DispatcherSynchronizationContext())

    let syslogSubject, subjectDisposable = SyslogReceiver.mkSyslogSubject ()
    use _cleanUpSubject = subjectDisposable

    use _cleanUpSub =
        syslogSubject.ObserveOn(SynchronizationContext.Current)
                     .Subscribe(
                        Action<_>(printfn "*** got one: %A")
                     )

    printfn "Waiting..."

    let frame = DispatcherFrame()
    ThreadPool.QueueUserWorkItem(fun _ ->   Test.testRawFocusUnits syslogSubject
                                            frame.Continue <- false) |> ignore
    Dispatcher.PushFrame(frame)

    //waitWithLiveDispatcher (Task.Delay(1000))

    0
