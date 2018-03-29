// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System

[<AutoOpen>]
module ExceptionHelpers =


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

    module private DisposeImpl =
        let emptyFirst = fun () -> ()

    /// Disposes the disposables indicated.
    let these isDisposed disposables = theseWith isDisposed disposables DisposeImpl.emptyFirst

    /// Wraps disposables for clean up.
    type private AnonymousDisposable (cleanUp: unit -> unit, disposables: IDisposable list) =
        let isDisposed = ref false
        interface IDisposable with
            member __.Dispose() = theseWith isDisposed disposables cleanUp

    /// Wraps disposables for clean up.
    let makeDisposable (cleanUp: unit -> unit) (disposables: IDisposable list) =
        new AnonymousDisposable(cleanUp, disposables) :> IDisposable

    /// Unpacks an option<IDisposable> into a list of zero or one disposables.
    let unpack (r: IDisposable option) =

        match r with
        | Some d -> [ d ]
        | None ->   []
