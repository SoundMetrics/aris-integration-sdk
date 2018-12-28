namespace SoundMetrics.Data

type Range<'t> = { Name: string; Min: 't; Max: 't }
with
    override rng.ToString() = sprintf "%s %A-%A" rng.Name rng.Min rng.Max

module Range =

    [<CompiledName("RangeContains")>]
    let contains value range =
        assert (range.Min <= range.Max)
        range.Min <= value && value <= range.Max

    let inline range<'T when 'T : comparison> name (min: 'T) (max: 'T) = { Name = name; Min = min; Max = max }


    let inline private isSubrangeOf<'T when 'T : comparison> (original : Range<'T>) subrange =

        original |> contains subrange.Min && original |> contains subrange.Max


    let inline private subrangeOf<'T when 'T : comparison> range (min : 'T) (max : 'T) =

        let subrange = { Name = range.Name; Min = min; Max = max }
        if not (subrange |> isSubrangeOf range) then
            invalidArg "min" "subrange falls outside original range"

        subrange


    let inline constrainRangeMax<'T when 'T : comparison> range (max : 'T) = subrangeOf range range.Min max
