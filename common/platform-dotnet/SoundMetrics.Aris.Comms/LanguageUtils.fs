// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

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


/// Functions to facilitate the disposal of disposable resources.
[<RequireQualifiedAccess>]
module Dispose =

    /// Disposes the disposables indicated after first calling the function 'cleanUp'.
    let theseWith (isDisposed: bool ref) (disposables: IDisposable seq) (cleanUp: unit -> unit) =
        if not !isDisposed then
            cleanUp ()
            isDisposed := true // after call to cleanUp code
            for d in disposables do
                if d <> null then d.Dispose()

    module private DisposeDetails =
        let emptyFirst = fun () -> ()

    /// Disposes the disposables indicated.
    let these isDisposed disposables = theseWith isDisposed disposables DisposeDetails.emptyFirst

    /// Wraps disposables for clean up.
    type private AnonymousDisposable (cleanUp: unit -> unit, disposables: IDisposable list) =
        let isDisposed = ref false
        interface IDisposable with
            member __.Dispose() = theseWith isDisposed disposables cleanUp

    /// Wraps disposables for clean up.
    let makeDisposable (cleanUp: unit -> unit) (disposables: IDisposable list) =
        new AnonymousDisposable(cleanUp, disposables) :> IDisposable
