// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open System

module internal Log =
    open Serilog
    open System.Diagnostics

    /// Use to log the first loading of an assembly. Useful when managed code is
    /// hosted by native code.
    let logLoad name =
        let callerStackFrame = StackFrame(1)
        let assyName = callerStackFrame.GetMethod().DeclaringType.Assembly.FullName
        Log.Information("Loading {name} - {timestamp} - {assyName}", name, DateTime.Now, assyName)

[<AutoOpen>]
module internal ExceptionHelpers =


    [<Literal>]
    let private MinJustificationLength = 5

    /// Ignores exceptions that happen in f; some critical exceptions, such as
    /// StackOverflowException, can't be ignored.
    let ignoreException (justification : string) (f : unit -> unit) =

        if String.IsNullOrWhiteSpace(justification) || justification.Length < MinJustificationLength then
            failwith (sprintf "Missing justification, %d or more characters" MinJustificationLength)

        try
            f()
        with
            _ -> ()

    // Extension methods for System.Exception
    type System.Exception with
        /// Returns the Message of this exception joined with the
        /// Messages of its inner exceptions.
        member ex.NestedMessage =

                let buf = System.Text.StringBuilder()

                let rec loop (ex : System.Exception) =
                    buf.Append(ex.Message) |> ignore
                    match ex.InnerException with
                    | null -> buf.ToString()
                    | inner -> loop inner
                loop ex
