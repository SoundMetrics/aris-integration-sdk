// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

module internal GraphBuilder =
    // ------------------------------------------------------------------------
    // Application-specific constructors for graph nodes (makeXXX).

    open GraphBinding

    // Kind of a custom job to bring in a reactive subject, so it's not in GraphBinding.
    let bindFrameSource (subject: IObservable<Frame>): Scaffold<WorkUnit> -> Scaffold<WorkUnit> =
        fun rhNode ->
            let tgt, leaves, disposablesIn = rhNode
            let linkableOutput = BufferBlock<WorkUnit> ()
            let disposables =
                [ linkableOutput.LinkTo(tgt, DataflowLinkOptions (PropagateCompletion = true))
                  subject.Subscribe(fun f -> linkableOutput.Post (WorkUnit.Frame f) |> ignore) ]
            linkableOutput :> ITargetBlock<WorkUnit>, leaves, disposables @ disposablesIn

    // ----------------------------------------------------------------------------
    // The graph is constructed from right to left, from leaf to root as
    // ISourceBlock.LinkTo() requires the right-hand node to exist.
    //
    // Data flows through the graph from left to right; e.g.,
    //
    //
    // (network) --> [buffer] --> [data-assembler] --+
    //                                               |
    //          +------------------------------------+
    //          V
    //   [image-processor] -->  [tee] --> [modeler] ---> ·
    //                            |
    //                            +-----> [recorder] --> ·

    let makeBuffer rhNode = (bindBuffer ()) rhNode

    let makeProcessorPipeline perfSink earlyFrameSpur rhNode =
        let makeState () = ref (FrameProcessing.ProcessPipelineState.Create ())
        (bindTransformWithState makeState (FrameProcessing.processPipeline perfSink earlyFrameSpur)) rhNode

    let makeRecorder recordedFrameIndexSpur rhNode =
        let makeState () = Recording.RecordingState.Create ()
        (bindActionWithState makeState (Recording.recordFrame recordedFrameIndexSpur)) rhNode

    let quit () =
        // 'Complete' this node when the Quit message comes through.
        let refAction: ActionBlock<ProcessedFrame> option ref = ref None
        let action = ActionBlock<ProcessedFrame>(fun pf -> 
            match pf.work with 
            | Quit ->
                match !refAction with
                | Some action -> action.Complete ()
                | None -> failwith "logic error, refAction is not initialized"
            | _ -> ()
        )

        refAction := Some action
        action :> ITargetBlock<ProcessedFrame>, [action.Completion], List.empty<IDisposable>

//-----------------------------------------------------------------------------

    /// Builds a simple recording pipeline.
    let buildSimpleRecordingGraph perfSink frameSource earlyFrameSpur recordedFrameIndexSpur =
        let ctor =
            bindFrameSource frameSource
            << makeBuffer
            << makeProcessorPipeline perfSink earlyFrameSpur
            << makeRecorder recordedFrameIndexSpur
            << quit
        ctor ()

//-----------------------------------------------------------------------------

    let quitWaitClean root =
        let quitGraph (root: ITargetBlock<WorkUnit>) = root.Post (WorkUnit.Quit) |> ignore
        let waitForGraphCompletion leafNodes = Task.WaitAll (leafNodes |> List.toArray)
        let cleanUpGraph (disposables: IDisposable list) =
            for d in disposables do
                if d <> null then d.Dispose()

        let target, leaves, disposables = root
        quitGraph target
        waitForGraphCompletion leaves
        cleanUpGraph disposables
