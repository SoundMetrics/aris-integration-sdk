module Main

open Serilog
open Serilog.Sinks.File
open SoundMetrics.Scripting
open System
open System.Reactive.Linq
open System.Threading
open System.Threading.Tasks
open System.Windows.Threading
open System.IO
open Parser.TestInput
open System.Windows.Interop

module private Details =
    [<Literal>]
    let LoggingTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"

    [<Literal>]
    let script1 = @"
# this is a comment
wait 1
label alpha
    focus 0
    wait 2
    focus 2
    wait 2
    focus 4
    wait 2
    focus 6
    wait 2
    #goto alpha

    "

    let runScript input
                  (syncContext : SynchronizationContext)
                  sn
                  (syslogMessages : IObservable<SyslogMessage>) =

        SynchronizationContext.SetSynchronizationContext(syncContext)

        match Parser.parse input with
        | Ok program -> program.Run(sn, ScriptBehavior.mkStandardBehavior())
        | Error e ->    Log.Error (sprintf "ERROR: line %u; %s" e.LineNumber e.Message)

open Argu
open Details

type SerialNumber = uint32
type Path = string

type Arguments =
    | Serial_Number of SerialNumber
    | Script of Path
    | Log of Path
    | DebugLog
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Serial_Number _ -> "specify an ARIS serial number."
            | Script _ -> "specify the location of the script file."
            | Log _ -> "specify a file to log to (optional)."
            | DebugLog -> "specify debug-level logging (optional)."

type LoggerConfiguration with
    member cfg.LogToFile logPath =
        match logPath with
        | Some logPath -> cfg.WriteTo.File(path = logPath, outputTemplate = LoggingTemplate)
        | None -> cfg
    member cfg.DebugLog debugLog =
        match debugLog with
        | Some _ -> cfg.MinimumLevel.Debug()
        | None -> cfg

[<EntryPoint>]
let main _argv =

    Thread.CurrentThread.Name <- "Main thread"

    let argParser = ArgumentParser.Create<Arguments>(programName = "VoyagerFocusTest.exe")
    try
        let argResults = argParser.ParseCommandLine()

        match argResults.TryGetResult(Serial_Number), argResults.TryGetResult(Script) with
        | None, _ ->
            eprintfn "ERROR: --serial-number is required"
            eprintfn ""
            eprintfn "%s" (argParser.PrintUsage())
            Environment.Exit(1)
        | Some _, None ->
            eprintfn "ERROR: --script is required"
            eprintfn ""
            eprintfn "%s" (argParser.PrintUsage())
        | Some sn, Some scriptPath ->
            let logPath = argResults.TryGetResult(Log)
            let debugLog = argResults.TryGetResult(DebugLog)

            Log.Logger <- (new LoggerConfiguration())
                            .WriteTo.Console(outputTemplate = LoggingTemplate)
                            .LogToFile(logPath)
                            .DebugLog(debugLog)
                            .CreateLogger()

            try
                Log.Information("Serial number={serialNumber}", sn)

                let syncContext = DispatcherSynchronizationContext()
                assert (not (isNull syncContext))
                SynchronizationContext.SetSynchronizationContext(syncContext)
                use syslogListener = new SyslogListener()

                let frame = DispatcherFrame()
                ThreadPool.QueueUserWorkItem(fun _ ->
                    try
                        try
                            Thread.CurrentThread.Name <- "Script thread"
                            use input = new StreamReader(scriptPath)
                            runScript input syncContext sn syslogListener.Messages
                        with
                            ex -> Log.Error("An error occurred: {msg}", ex.Message)
                                  Log.Error("{stackTrace}", ex.StackTrace)
                    finally
                        frame.Continue <- false
                ) |> ignore
                Dispatcher.PushFrame(frame)
            with
                ex -> Log.Error("ERROR: {msg}", ex.Message)
    with
        ex ->
            eprintfn "ERROR: %s" ex.Message

    System.Environment.Exit(0) // TODO

    //SyslogReceiver.test () |> ignore

    //SynchronizationContext.SetSynchronizationContext(DispatcherSynchronizationContext())

    //use syslogListener = new SyslogListener()

    //let frame = DispatcherFrame()
    //ThreadPool.QueueUserWorkItem(fun _ ->   Thread.CurrentThread.Name <- "Test.testRawFocusUnits"
    //                                        Test.testRawFocusUnits syslogListener.Messages
    //                                        frame.Continue <- false) |> ignore
    //Dispatcher.PushFrame(frame)

    0
