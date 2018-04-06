module ProgramActions

// All actions defined here must be listed below in `allActions`.

open Argu
open ProgramArgs
open Serilog
open TestInputs
open TestList

type ProgramActionResult = Skipped | Performed
type ProgramAction = ParseResults<ProgramArgs> -> ProgramActionResult

let runListTests (args : ParseResults<ProgramArgs>) =

    if args.Contains List_Tests then
        Log.Information("Listing available tests:")
        getTestNames() |> Seq.iter (fun testName -> Log.Information("  Available test: {testName}", testName))
        Performed
    else
        Skipped

let runAllTests (args : ParseResults<ProgramArgs>) =

    if args.Contains All then
        Log.Information("Run all tests")
        let inputs = {
            SerialNumber = if args.Contains Serial_Number then
                                Some (int (args.GetResult(Serial_Number)))
                           else
                                None
        }
        TestList.runAllTests inputs
        Performed
    else
        Skipped

let runBackstop (args : ParseResults<ProgramArgs>) =
    Log.Verbose("Running backstop action")
    args.Parser.PrintUsage() |> printfn "%s"
    Performed

let allActions : ProgramAction array = [|
    runListTests
    runAllTests
    runBackstop // This action must be last, it picks up the case where nothing is requested.
|]
