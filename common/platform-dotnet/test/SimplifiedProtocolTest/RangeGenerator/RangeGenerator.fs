﻿namespace RangeGenerator

open System.Collections
open System.Collections.Generic

type RangeGeneratorAdvance<'T> = 'T -> 'T option
type RangeGeneratorIsDone<'T> = 'T -> 'T -> bool

type public RangeGenerator<'T>
    (start: 'T,
     endInclusive: 'T,
     advance: RangeGeneratorAdvance<'T>,
     isDone: RangeGeneratorIsDone<'T>) =

    interface IEnumerable<'T> with
        member this.GetEnumerator(): IEnumerator =
            new RangeGeneratorEnumerator<'T>(this) :> IEnumerator

        member this.GetEnumerator(): IEnumerator<'T> =
            new RangeGeneratorEnumerator<'T>(this) :> IEnumerator<'T>

    member internal __.Start = start
    member internal __.EndInclusive = endInclusive
    member internal __.Advance = advance
    member internal __.IsDone = isDone

and internal EnumeratorState<'T> =
    | BeforeFirst
    | Enumerating of value: 'T
    | Done

and internal RangeGeneratorEnumerator<'T> (generator: RangeGenerator<'T>) =

    let mutable state = BeforeFirst

    interface IEnumerator<'T> with
        member this.Current: 'T =
            match state with
            | Enumerating value -> value

            | BeforeFirst
            | Done ->
                failwith "Current value is undefined"

        member this.Current: obj = (this :> IEnumerator<'T>).Current :> obj

        member __.Dispose(): unit = ()

        member this.MoveNext(): bool =
            match state with
            | BeforeFirst ->
                let firstValue = generator.Start
                state <- Enumerating firstValue
                true

            | Enumerating value ->
                match generator.Advance value with
                | Some newValue ->
                    state <- Enumerating newValue
                    true
                | None ->
                    state <- Done
                    false

            | Done -> false

        member __.Reset(): unit = state <- BeforeFirst
