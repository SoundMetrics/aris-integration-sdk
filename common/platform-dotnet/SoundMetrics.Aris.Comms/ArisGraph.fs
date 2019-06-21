namespace SoundMetrics.Aris.Comms.Experimental

open System
open System.Reactive.Subjects
open SoundMetrics.Dataflow.Graph
open SoundMetrics.Aris.Comms

module ArisGraph =

    module internal Details =

        let reorderSamples input =

            match input with
            | ArisFrame (RawFrame f) -> failwith "nyi"
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> input // ignore it

        let subtractBackground input =

            match input with
            | ArisFrame (OrderedFrame f) -> failwith "nyi"
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> input // ignore it

        let recordBgs input : unit =

            match input with
            | ArisFrame (FrameWithBgs f) -> failwith "nyi"
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> () // ignore it

        let recordFile input : unit =

            match input with
            | ArisFrame (FrameWithBgs f) -> failwith "nyi"
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> () // ignore it

        let toFinishedFrame input : FinishedFrame =

            match input with
            | ArisFrame (FrameWithBgs f) -> f
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> failwith "Unexpected command"

        let isFinishedFrame input =

            match input with
            | ArisFrame (FrameWithBgs f) -> true
            | _ -> false

        let takeSnapshot (mostRecent: FinishedFrame voption ref) input : unit =

            match input with
            | ArisFrame (FrameWithBgs f) -> mostRecent := ValueSome f
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand TakeSnapshot ->
                match !mostRecent with
                | ValueSome f -> failwith "nyi"
                | ValueNone -> () // ignore; no frame to save
            | GraphCommand _ -> () // ignore

    open Details

    [<CompiledName("Build")>]
    let build
            (name: string)
            (frameObservable: IObservable<Frame>)
            (displaySubject: ISubject<FinishedFrame>)
            : GraphHandle<ArisGraphInput> =

        let graphRoot =
            let frameToInput frame = ArisFrame (RawFrame frame)
            bufferObservable 100 frameObservable frameToInput
                ^|> transform (reorderSamples >> subtractBackground)
                ^|> tee
                    [
                        buffer 0
                            ^|> filter isFinishedFrame
                            ^|> transform toFinishedFrame
                            ^|> escapeTo displaySubject

                        buffer 0
                            ^|> serialsink
                                [
                                    takeSnapshot (ref ValueNone)
                                    recordBgs
                                    recordFile
                                ]
                    ]

        new GraphHandle<_>(name, graphRoot)
