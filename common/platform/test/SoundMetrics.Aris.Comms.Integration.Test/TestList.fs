module TestList

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Serilog
open System
open TestInputs

module private TestListDetails =

    type TestResult =   Result<unit, string>
    type TestFunc =     TestInputs -> TestResult
    type TestEntry =    Expr<TestFunc>

    let tests : TestEntry array =
        [|
            <@ BasicConnection.testBasicConnection @>
            <@ FrameProcessingStats.frameProcessingStats @>
            <@ RecordingStats.recordingStats @>
        |]

    let getTestName testEntry =

        match testEntry with
        | Lambda (var, expr) ->
            match expr with
            | Call (_, methodInfo, _) -> methodInfo.Name
            | _ -> failwith "Unexpected result from Call"
        | _ -> failwith "Unexpected result from Lambda"

    let separator = String('-', 80)

    let runTestEntry testEntry (inputs : TestInputs) =

        match testEntry with
        | Lambda (var, expr) ->
            match expr with
            | Call (_, methodInfo, _) ->
                let testName = getTestName testEntry
                Log.Information(separator)
                Log.Information("Starting test {testName}", testName)
                let result = methodInfo.Invoke(null, [| inputs |]) :?> TestResult
                match result with
                | Ok () -> Log.Information("Test {testName} succeeded", testName)
                | Error msg -> Log.Error("Test {testName} failed: '{msg}'", testName, msg)
                Log.Information("Ending test {testName}", testName)
            | _ -> failwith "Unexpected result from Call"
        | _ -> failwith "Unexpected result from Lambda"

    let testMap = tests |> Seq.map (fun entry -> (getTestName entry, entry))
                        |> Map.ofSeq

open TestListDetails

let getTestNames () =

    tests |> Seq.map getTestName

let runTest name inputs =

    match testMap.TryFind(name) with
    | Some entry -> runTestEntry entry inputs
    | None -> failwithf "Couldn't find a test named '%s'" name

let runAllTests inputs =

    for entry in tests do
        runTestEntry entry inputs
