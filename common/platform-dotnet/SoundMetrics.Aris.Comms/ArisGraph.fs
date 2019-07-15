namespace SoundMetrics.Aris.Comms.Experimental

open System
open System.Reactive.Subjects
open SoundMetrics.Dataflow.Graph
open SoundMetrics.Aris.Comms
open SoundMetrics.Aris.ReorderCS
open SoundMetrics.NativeMemory

module ArisGraph =

    module internal Details =

        let reorderSamples input =

            match input with
            | ArisFrame (RawFrame f) ->

                let buildHistogram source length =
                    FrameHistogram.Generate(source, length)

                let frameGeometry = ArisFrameGeometry.FromFrame(f)
                let orderedData =
                    let reorder = fun src dest ->
                        Reorder.ReorderFrame(
                            frameGeometry.PingMode,
                            frameGeometry.PingsPerFrame,
                            frameGeometry.BeamCount,
                            frameGeometry.SampleCount,
                            src,
                            dest
                        )
                    NativeBuffer.transform reorder f.SampleData

                let orderedFrame =
                    {
                        ArisOrderedFrame.Header = ArisFrameHeaderBindable f.Header
                        FrameGeometry = frameGeometry
                        SampleData = orderedData
                        Histogram = f.SampleData |> NativeBuffer.map buildHistogram
                    }
                (ArisFrame (OrderedFrame orderedFrame))
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> input // ignore it

        let subtractBackground input =

            match input with
            | ArisFrame (OrderedFrame f) ->
                // TODO BGS operations
                let finishedFrame =
                    {
                        ArisFinishedFrame.Header = f.Header
                        FrameGeometry = f.FrameGeometry
                        SampleData = f.SampleData
                        Histogram = f.Histogram
                    }
                ArisFrame (FrameWithBgs finishedFrame)
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> input // ignore it

        let recordBgs input : unit =

            match input with
            | ArisFrame (FrameWithBgs f) ->
                // TODO BGS operations
                ()
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> () // ignore it

        let recordFile input : unit =

            match input with
            | ArisFrame (FrameWithBgs f) ->
                // TODO record file
                ()
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> () // ignore it

        let toFinishedFrame input : ArisFinishedFrame =

            match input with
            | ArisFrame (FrameWithBgs f) -> f
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand _ -> failwith "Unexpected command"

        let isFinishedFrame = function

            | ArisFrame (FrameWithBgs f) -> true
            | _ -> false

        let takeSnapshot (mostRecent: ArisFinishedFrame voption ref) input : unit =

            match input with
            | ArisFrame (FrameWithBgs f) -> mostRecent := ValueSome f
            | ArisFrame f -> failwithf "Unexpected frame type: %s" (f.GetType().Name)
            | GraphCommand TakeSnapshot ->
                match !mostRecent with
                | ValueSome f ->
                    // TODO take a snapshot
                    ()
                | ValueNone -> () // ignore; no frame to save
            | GraphCommand _ -> () // ignore

    open Details

    [<CompiledName("Build")>]
    let build
            (name: string)
            (frameObservable: IObservable<RawFrame>)
            (displaySubject: ISubject<ArisFinishedFrame>)
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
