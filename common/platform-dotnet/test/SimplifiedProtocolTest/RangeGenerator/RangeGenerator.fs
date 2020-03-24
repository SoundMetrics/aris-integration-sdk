namespace SoundMetrics.Data

open System
open System.Collections
open System.Collections.Generic

module RangeGenerator =

    type RangeGeneratorAdvance<'T> = 'T -> 'T

    type EnumeratorState<'T> =
        | BeforeFirst
        | Enumerating of value: 'T
        | Done

    type EnumeratorStateOption<'T> =
        | BeforeFirstOption
        | EnumeratingOption of value: 'T option
        | DoneOption

    /// Support for makeRange<'T>.
    type RangeGeneratorEnumerator<'T when 'T :> IComparable<'T> and 'T : comparison>
                (start: 'T
               , endInclusive: 'T
               , advance: 'T -> 'T) =

        let mutable state = BeforeFirst

        let isPastEnd (value: 'T) = value.CompareTo(endInclusive) > 0

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
                    let firstValue = start
                    state <- Enumerating firstValue
                    true

                | Enumerating value ->
                    match advance value with
                    | newValue when isPastEnd newValue ->
                        state <- Done
                        false
                    | newValue ->
                        state <- Enumerating newValue
                        true

                | Done -> false

            member __.Reset(): unit = state <- BeforeFirst

    /// Creates an enumerable range of 'T.
    let inline makeRange<'T when 'T :> IComparable<'T> and 'T : comparison>
                (start: 'T)
                (endInclusive: 'T)
                (advance: 'T -> 'T)
                : IEnumerable<'T> =

        if endInclusive < start then
            raise (ArgumentOutOfRangeException("endInclusive", "end must be >= start"))

        {
            new IEnumerable<'T> with
                member __.GetEnumerator(): IEnumerator =
                    new RangeGeneratorEnumerator<'T>(start, endInclusive, advance) :> IEnumerator
                member __.GetEnumerator(): IEnumerator<'T> =
                    new RangeGeneratorEnumerator<'T>(start, endInclusive, advance) :> IEnumerator<'T>
        }

    /// Support for makeOptionalRange<'T>.
    type RangeGeneratorEnumeratorWithOption<'T when 'T :> IComparable<'T> and 'T : comparison>
                    (start: 'T
                   , endInclusive: 'T
                   , advance: 'T -> 'T) =

        let mutable state = BeforeFirstOption

        let isPastEnd (value: 'T) = value.CompareTo(endInclusive) > 0

        interface IEnumerator<'T option> with
            member __.Current: 'T option =
                match state with
                | EnumeratingOption value -> value

                | BeforeFirstOption
                | DoneOption ->
                    failwith "Current value is undefined"

            member this.Current: obj = (this :> IEnumerator<'T option>).Current :> obj

            member __.Dispose(): unit = ()

            member __.MoveNext(): bool =
                match state with
                | BeforeFirstOption ->
                    state <- EnumeratingOption None
                    true

                | EnumeratingOption value ->
                    match value with
                    | None ->
                        state <- EnumeratingOption (Some start)
                        true
                    | Some v ->
                        match advance v with
                        | newValue when isPastEnd newValue ->
                            state <- DoneOption
                            false
                        | newValue ->
                            state <- EnumeratingOption (Some newValue)
                            true

                | DoneOption -> false

            member __.Reset(): unit = state <- BeforeFirstOption

    /// Creates an enumerable range of 'T option; Option<'T>.None leads the output.
    let inline makeOptionalRange<'T when 'T :> IComparable<'T> and 'T : comparison>
                (start: 'T)
                (endInclusive: 'T)
                (advance: 'T -> 'T)
                : IEnumerable<'T option> =

        if endInclusive < start then
            raise (ArgumentOutOfRangeException("endInclusive", "end must be >= start"))

        {
            new IEnumerable<'T option> with
                member __.GetEnumerator(): IEnumerator =
                    new RangeGeneratorEnumeratorWithOption<'T>(start, endInclusive, advance) :> IEnumerator
                member __.GetEnumerator(): IEnumerator<'T option> =
                    new RangeGeneratorEnumeratorWithOption<'T>(start, endInclusive, advance) :> IEnumerator<'T option>
        }
