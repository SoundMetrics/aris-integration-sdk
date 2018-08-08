// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System

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
