namespace SoundMetrics.Data

type Range<'t> = { Min: 't; Max: 't }
with
    override rng.ToString() = sprintf "%A-%A" rng.Min rng.Max

module Range =

    [<CompiledName("RangeContains")>]
    let contains value range =
        if range.Min > range.Max then
            failwith "Negative range is not allowed."

        range.Min <= value && value <= range.Max

    let inline range<'T when 'T : comparison> (min: 'T) (max: 'T) = { Min = min; Max = max }


    let inline private isSubrangeOf<'T when 'T : comparison> (original : Range<'T>) subrange =

        original |> contains subrange.Min && original |> contains subrange.Max


    let inline private subrangeOf<'T when 'T : comparison> range (min : 'T) (max : 'T) =

        let subrange = { Min = min; Max = max }
        if not (subrange |> isSubrangeOf range) then
            invalidArg "min" "subrange falls outside original range"

        subrange


    let inline constrainTo<'T when 'T : comparison> (range : Range<'T>) (t : 'T) =

        max range.Min (min range.Max t)


    let inline constrainRangeMax<'T when 'T : comparison> range (max : 'T) = subrangeOf range range.Min max
