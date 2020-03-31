// Learn more about F# at http://fsharp.org

open Argu
open SimpleSim
open System
open System.IO

[<AutoOpen>]
module internal MainDetails =

    let printWelcomeMessage (sink : TextWriter) =
        sink.WriteLine("SimpleSim - simulator for ARIS Simplified Protocol")

    let getUsage () =
        ArgumentParser
            .Create<ProgramArguments>(programName = "simplesim.exe")
            .PrintUsage()

[<EntryPoint>]
let main argv =

    printWelcomeMessage Console.Out

    match ProgramArguments.GetProgramArguments argv with
    | Arguments args -> failwith "NYI"

    | Message message ->
        Console.Out.WriteLine(message)
        getUsage() |> Console.Out.WriteLine

    | ShowUsage ->
        getUsage() |> Console.Out.WriteLine

    0 // return an integer exit code
