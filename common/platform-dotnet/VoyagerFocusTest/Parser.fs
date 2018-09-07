module Parser

open Serilog
open System
open System.IO

type FocusRange = float32

module TestInput =
    open SoundMetrics.Aris.Comms
    open ArisSupport
    open SoundMetrics.Common
    open System.Threading

    type LabelName = string

    type Statement =
        | Wait  of TimeSpan
        | Label of LabelName
        | Goto  of LabelName
        | Focus of FocusRange

    type LineNumber = uint32

    type Token =
        | Wait
        | Label
        | Goto
        | Focus
        | Float of float32
        | String of string

    let (|Number|_|) (s : string) =
        match Single.TryParse(s) with
        | true, f -> Some (Float f)
        | _ -> None

    let tokenize (line : string) =

        let tokenizeSplits (splits : string array) : Token list =

            splits |> Seq.map (fun s ->
                        let uc = s.ToUpperInvariant()
                        match uc with
                        | "WAIT" -> Wait
                        | "LABEL" -> Label
                        | "GOTO" -> Goto
                        | "FOCUS" ->Focus
                        | _ -> match s with
                               | Number token -> token
                               | _ -> String s
                   )
                   |> Seq.toList

        line.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
            |> tokenizeSplits

    type ParsedLine = {
        LineNumber  : LineNumber
        Label       : string option
        Statement   : Statement
    }

    type private LabelInfo = {
        Name : LabelName
        ParsedLine : ParsedLine
    }

    let getDuplicateLabel (statements : ParsedLine seq) =

        let getLine (l : LabelInfo) = l.ParsedLine.LineNumber

        let duplicates =
            statements |> Seq.map (fun pl ->
                            match pl.Statement with
                            | Statement.Label label -> Some { Name = label; ParsedLine = pl }
                            | _ -> None)
                       |> Seq.choose id
                       |> Seq.groupBy (fun l -> l.Name)
                       |> Seq.map (fun (key, ls) ->
                           key, ls |> Seq.length, ls |> Seq.map getLine)
                       |> Seq.filter (fun (_, count, _) -> count > 1)
                       |> Seq.cache
        if duplicates |> Seq.isEmpty then
            None
        else
            let key, _, lineNos = duplicates |> Seq.head
            Some (key, lineNos |> Seq.toArray)

    let makeLabelMap (parsedLines : ParsedLine seq) =

        parsedLines
            |> Seq.map (fun pl ->
                match pl.Statement with
                | Statement.Label label -> Some (label, pl.LineNumber)
                | _ -> None)
            |> Seq.choose id
            |> Map.ofSeq


    let findStatementByLabel targetLabel (parsedLines : ParsedLine list) =

        parsedLines |> Seq.mapi (fun i t -> i, t)
                    |> Seq.filter (fun (i, pl) -> match pl.Label with
                                                  | Some label -> label = targetLabel
                                                  | None -> false)
                    |> Seq.head
                    |> fst


    type IScriptBehavior =
        abstract member Focus : ArisConduit -> FocusRange -> unit
        abstract member Wait : ArisConduit -> TimeSpan -> unit

    type PCAction = UpdatePC | ReadyPC

    type Script (parsedLines : ParsedLine list) =

        let labelMap = makeLabelMap parsedLines

        let getConduit (sn : SerialNumber)=

            Log.Information("Looking for ARIS {sn}'s beacon", sn)
            use availables = BeaconListener.CreateForArisExplorerAndVoyager(TimeSpan.FromSeconds(15.0))
            getArisBeacon availables sn
            |> Option.map (fun beacon ->
                beacon,
                    new ArisConduit(
                            AcousticSettings.DefaultAcousticSettingsFor(beacon.SystemType),
                            sn,
                            FrameStreamReliabilityPolicy.DropPartialFrames))

        member __.Run (sn : SerialNumber,
                       behavior : IScriptBehavior) =

            assert (not (isNull SynchronizationContext.Current))

            if sn = 0u then
                invalidArg "sn" "must be non-zero"
            if isNull (box behavior) then
                invalidArg "behavior" "must not be null"

            match getConduit sn with
            | None -> Log.Error("Could not find beacon for ARIS {sn}", sn)
            | Some (beacon, conduit) ->
                Log.Information("Found ARIS {sn}", sn)
                let focusRange = FocusRange.calculateAvailableFocusRange
                                    beacon.SystemType 21.0<degC> Salinity.Fresh false
                Log.Information("Focus range for ARIS {sn}: {focusRange}", sn, focusRange)

                let mutable keepGoing = true
                let mutable idxNext = 0
                let behaviorContext = conduit

                Log.Information("Running script...")

                while keepGoing do
                    let next = parsedLines.[idxNext]
                    let pcAction =
                        match next.Statement with
                        | Statement.Wait duration ->
                            behavior.Wait behaviorContext duration
                            UpdatePC
                        | Statement.Label _ -> UpdatePC
                        | Statement.Goto label ->
                            idxNext <- findStatementByLabel label parsedLines
                            ReadyPC
                        | Statement.Focus fu ->
                            behavior.Focus behaviorContext fu
                            UpdatePC

                    if pcAction = UpdatePC then
                        idxNext <- idxNext + 1
                        if idxNext >= parsedLines.Length then
                            keepGoing <- false

                Log.Information("Script completed.")

open TestInput

type ParseError = {
    LineNumber : LineNumber
    Message : string
}

let parse (input : TextReader) : Result<Script, ParseError> =

    Log.Information("Parsing script...")

    let rec parseLine lineNo (scriptStatements : ParsedLine list) : Result<ParsedLine list, ParseError> =

        let line = input.ReadLine()
        if isNull line then
            if scriptStatements |> List.isEmpty then
                let msg = "No executable statements were found."
                Error { Message = msg; LineNumber = lineNo }
            else
                Log.Information("Completed parsing script.")
                Ok scriptStatements
        else
            let line = line.Trim()
            if line.StartsWith("#") || String.IsNullOrWhiteSpace(line) then
                parseLine (lineNo + 1u) scriptStatements
            else
                let tokens = tokenize line
                let statement =
                    match tokens with
                    | [ Wait; Float f ] -> Ok (Statement.Wait (TimeSpan.FromSeconds(float f)))
                    | [ Label; String label ] -> Ok (Statement.Label label)
                    | [ Goto; String label ] -> Ok (Statement.Goto label)
                    | [ Focus; Float f ] ->
                        if f >= 0.0f then
                            Ok (Statement.Focus f)
                        else
                            Error { Message = sprintf "Focus must be >= 0): '%f'" f
                                    LineNumber = lineNo }
                    | _ ->  let msg =
                                let tokenStrings = tokens |> Seq.map (fun t -> sprintf "%A" t)
                                let stokens = String.Join(" ", tokenStrings)
                                sprintf "Unexpected token order: %s" stokens
                            Error { Message = msg; LineNumber = lineNo }
                match statement with
                | Ok st ->
                    let pl =
                        match st with
                        | Statement.Label label -> { LineNumber = lineNo; Label = Some label; Statement = st }
                        | _ -> { LineNumber = lineNo; Label = None; Statement = st }
                    parseLine (lineNo + 1u) (scriptStatements @ [ pl ])
                | Error error -> Error error

    match parseLine 1u [] with
    | Ok statements ->
        match getDuplicateLabel statements with
        | None -> Ok (Script statements)
        | Some statement ->
            match statement with
            | (label, lineNos) ->
                Error { Message = sprintf "Duplicate labels found: '%s' on lines %s" label
                                    (String.Join(", ", lineNos |> Seq.map string))
                        LineNumber = 1u }
    | Error msg -> Error msg


