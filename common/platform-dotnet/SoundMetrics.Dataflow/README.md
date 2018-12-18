# SoundMetrics.Dataflow

This assembly brings easy assembly of TPL dataflow graphs.
It's written in F#, but given some examples you should pick
it up.

## Example

This little program builds a graph with three branches.
Note that the end of each branch is terminated with a `leaf` node.

```F#
open SoundMetrics.Dataflow.Graph

let intToString i = i.ToString()
let intToFloat i = float i
let gtZero i = i > 0
let perCent i = float i / 100.0

let makeGraph () =
    let graph =
        buffer 100 ^|>
            tee [
                buffer 10 ^|> transform intToString ^|> leaf(printfn "string %s")

                buffer 10 ^|> transform perCent     ^|> leaf(printfn "/100 %f")

                buffer 10 ^|> filter gtZero         ^|> leaf(printfn ">0 %d")
            ]
    new GraphHandle<_>(graph)


[<EntryPoint>]
let main _argv =

    use gh = makeGraph()

    seq { -1 .. +1 } |> Seq.iter (fun i -> gh.Post(i) |> ignore)

    Thread.Sleep(1000)

    let sw = Stopwatch.StartNew()
    if (gh.CompleteAndWait(TimeSpan.FromSeconds(0.5))) then
        printfn "Time to complete: %A" sw.Elapsed
    else
        eprintfn "Timed out waiting for completion."

    0
```
