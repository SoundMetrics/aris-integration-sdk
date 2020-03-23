namespace RangeGenerator

open System
open System.Collections
open System.Collections.Generic

type RangeGeneratorAdvance<'T> = 'T -> 'T option

type public RangeGenerator<'T when 'T :> IComparable<'T>>
    (start: 'T,
     endInclusive: 'T,
     advance: RangeGeneratorAdvance<'T>) =

    do
        if endInclusive.CompareTo(start) > 0 then
            raise (ArgumentOutOfRangeException("endInclusive", "end must be >= start"))

    interface IEnumerable<'T> with
        member this.GetEnumerator(): IEnumerator =
            new RangeGeneratorEnumerator<'T>(this) :> IEnumerator

        member this.GetEnumerator(): IEnumerator<'T> =
            new RangeGeneratorEnumerator<'T>(this) :> IEnumerator<'T>

    member internal __.Start = start
    member internal __.EndInclusive = endInclusive
    member internal __.Advance = advance

and internal EnumeratorState<'T> =
    | BeforeFirst
    | Enumerating of value: 'T
    | Done

and internal RangeGeneratorEnumerator<'T when 'T :> IComparable<'T>>
        (generator: RangeGenerator<'T>) =

    let mutable state = BeforeFirst

    let isPastEnd (value: 'T) = value.CompareTo(generator.EndInclusive) > 0

    interface IEnumerator<'T> with
        member __.Current: 'T =
            match state with
            | Enumerating value -> value

            | BeforeFirst
            | Done ->
                failwith "Current value is undefined"

        member this.Current: obj = (this :> IEnumerator<'T>).Current :> obj

        member __.Dispose(): unit = ()

        member __.MoveNext(): bool =
            match state with
            | BeforeFirst ->
                let firstValue = generator.Start
                state <- Enumerating firstValue
                true

            | Enumerating value ->
                match generator.Advance value with
                | Some newValue when isPastEnd newValue ->
                    state <- Done
                    false
                | Some newValue ->
                    state <- Enumerating newValue
                    true
                | None ->
                    state <- Done
                    false

            | Done -> false

        member __.Reset(): unit = state <- BeforeFirst
