// Learn more about F# at http://fsharp.org

open Argu
open ProgramArgs
open Serilog
open Serilog.Core
open Serilog.Events
open System.Reflection

[<Literal>]
let LoggingTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"

let applyAction (args : ParseResults<ProgramArgs>) =

    ()

[<EntryPoint>]
let main argv =

    let programName = Assembly.GetEntryAssembly().Location |> System.IO.Path.GetFileName

    let argParser = ArgumentParser.Create<ProgramArgs>(programName = programName)

    if argv.Length = 0 then
        argParser.PrintUsage() |> printfn "%s"
        System.Environment.Exit(1)

    let args = argParser.Parse(argv)


    let loggingLevel = if args.Contains Verbose then LogEventLevel.Verbose else LogEventLevel.Information
    Log.Logger <-
        let logLevel =
            let switch = LoggingLevelSwitch()
            switch.MinimumLevel <- loggingLevel
            switch
                            
        (new LoggerConfiguration())
            .MinimumLevel.ControlledBy(logLevel)
            .WriteTo.Console(outputTemplate = LoggingTemplate)
            .CreateLogger()

    Log.Information("Starting {programName}", programName)
    Log.Information("Logging level is {loggingLevel}", loggingLevel)

    let mutable actionState = ProgramActions.Skipped
    for action in ProgramActions.allActions do
        match actionState with
        | ProgramActions.Skipped -> actionState <- action args
        | ProgramActions.Performed -> ()

    0
