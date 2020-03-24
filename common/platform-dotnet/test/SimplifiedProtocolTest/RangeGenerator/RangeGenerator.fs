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

//open RangeGenerator

//type public RangeGenerator<'T when 'T :> IComparable<'T>>
//    (start: 'T,
//     endInclusive: 'T,
//     advance: RangeGeneratorAdvance<'T>) =

//    do
//        if endInclusive.CompareTo(start) < 0 then
//            raise (ArgumentOutOfRangeException("endInclusive", "end must be >= start"))

//    interface IEnumerable<'T> with
//        member this.GetEnumerator(): IEnumerator =
//            new RangeGeneratorEnumerator<'T>(this) :> IEnumerator

//        member this.GetEnumerator(): IEnumerator<'T> =
//            new RangeGeneratorEnumerator<'T>(this) :> IEnumerator<'T>

//    member public this.GetEnumerator(): IEnumerator<'T> =
//        (this :> IEnumerable<'T>).GetEnumerator()

//    member internal __.Start = start
//    member internal __.EndInclusive = endInclusive
//    member internal __.Advance = advance

//and internal EnumeratorState<'T> =
//    | BeforeFirst
//    | Enumerating of value: 'T
//    | Done

//and internal RangeGeneratorEnumerator<'T when 'T :> IComparable<'T>>
//        (generator: RangeGenerator<'T>) =

//    let mutable state = BeforeFirst

//    let isPastEnd (value: 'T) = value.CompareTo(generator.EndInclusive) > 0

//    interface IEnumerator<'T> with
//        member __.Current: 'T =
//            match state with
//            | Enumerating value -> value

//            | BeforeFirst
//            | Done ->
//                failwith "Current value is undefined"

//        member this.Current: obj = (this :> IEnumerator<'T>).Current :> obj

//        member __.Dispose(): unit = ()

//        member __.MoveNext(): bool =
//            match state with
//            | BeforeFirst ->
//                let firstValue = generator.Start
//                state <- Enumerating firstValue
//                true

//            | Enumerating value ->
//                match generator.Advance value with
//                | newValue when isPastEnd newValue ->
//                    state <- Done
//                    false
//                | newValue ->
//                    state <- Enumerating newValue
//                    true

//            | Done -> false

//        member __.Reset(): unit = state <- BeforeFirst
