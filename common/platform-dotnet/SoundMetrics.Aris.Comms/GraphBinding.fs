// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

// Support for binding functions to the scaffolding used to create the
// dataflow graph.

module internal GraphBinding =

    open System
    open System.Threading.Tasks
    open System.Threading.Tasks.Dataflow

    // Track the leaf nodes so we can know when the graph has drained.
    type LeafNodes = Task list

    // Defines the scaffolding used to link one graph node to the next.
    // The target is the ITargetBlock<'T> that the constructor has built to accept
    // input. The leaf nodes are tracked so we can determine when the processing has
    // finished. The list of disposables is for cleaning up resources when we're done.
    type Scaffold<'Target> = ITargetBlock<'Target> * LeafNodes * (IDisposable list)

    let bindBuffer (): Scaffold<'T> -> Scaffold<'T> =
        fun rhNode ->
            let tgt, leaves, disposablesIn = rhNode
            let linkableOutput = BufferBlock<'T> ()
            let disposable = linkableOutput.LinkTo(tgt, DataflowLinkOptions (PropagateCompletion = true))
            linkableOutput :> ITargetBlock<'T>, leaves, disposable :: disposablesIn

    // Bind a function to the scaffolding needed to function within the dataflow grah.
    // Because the graph is constructed from right to left (leaf to root), the output
    // of the binder is the reverse direction of the transform.
    //
    //      transform:  'T -> 'U
    //      bound ctor: Scaffold<'U> -> Scaffold<'T>

    let bindTransform (transform: 'T -> 'U): Scaffold<'U> -> Scaffold<'T> =
        fun rhNode ->
            let tgt, leaves, disposablesIn = rhNode
            let transform = TransformBlock (transform)
            let disposable = transform.LinkTo (tgt, DataflowLinkOptions (PropagateCompletion = true))
            transform :> ITargetBlock<'T>, leaves, disposable :: disposablesIn

    let bindTransformWithState (makeState: unit -> 'State)
                               (transform: 'State -> 'T -> 'U)
            : Scaffold<'U> -> Scaffold<'T> =
        fun rhNode ->
            let state = makeState ()
            let transform = transform state
            let tgt, leaves, disposablesIn = rhNode
            let transform = TransformBlock (transform)
            let disposable = transform.LinkTo (tgt, DataflowLinkOptions (PropagateCompletion = true))
            transform :> ITargetBlock<'T>, leaves, disposable :: disposablesIn

    // Binding an action implies that there is no transform. However, this node
    // is not a leaf node so it can be freely moved within the graph. The implied
    // transformation is identity.
    let bindAction (action: 'T -> unit): Scaffold<'T> -> Scaffold<'T> =
        fun rhNode ->
            let linkableOutput = BufferBlock<'T>()
            let actionBlock = ActionBlock(fun t -> action t ; linkableOutput.Post (t) |> ignore)
            actionBlock.Completion.ContinueWith(fun _ -> linkableOutput.Complete()) |> ignore
                        
            let tgt, leaves, disposablesIn = rhNode
            let disposable = linkableOutput.LinkTo (tgt, DataflowLinkOptions(PropagateCompletion = true))
            actionBlock :> ITargetBlock<'T>, leaves, disposable :: disposablesIn

    let bindActionWithState (makeState: unit -> 'State)
                            (action: 'State -> 'T -> unit)
            : Scaffold<'T> -> Scaffold<'T> =
        fun rhNode ->
            let state = makeState ()
            let action = action state
            let linkableOutput = BufferBlock<'T>()
            let actionBlock = ActionBlock(fun t -> action t ; linkableOutput.Post (t) |> ignore)
            actionBlock.Completion.ContinueWith(fun _task -> linkableOutput.Complete()) |> ignore

            let tgt, leaves, disposablesIn = rhNode
            let disposable = linkableOutput.LinkTo (tgt, DataflowLinkOptions(PropagateCompletion = true))
            actionBlock :> ITargetBlock<'T>, leaves, disposable :: disposablesIn

    let terminate<'T> () =
        let action = ActionBlock<'T>(fun _ -> ())
        action :> ITargetBlock<'T>, [action.Completion], List.empty<IDisposable>

    let makeTee (partCtors: (unit -> (#ITargetBlock<'T> * LeafNodes * IDisposable list)) list) =
        fun () ->
            let targets, leaves, disposables =
                partCtors |> List.fold
                    (fun state ctor ->
                        let targetsIn, leavesIn, disposablesIn = state
                        let newT, newL, newDs = ctor ()
                        newT :: targetsIn, leavesIn @ newL,
                            disposablesIn @ newDs
                    )
                    ([], List.empty<Task>, List.empty<IDisposable>) 
            let buffer = BufferBlock<'T>()
            buffer.Completion.ContinueWith(fun _task -> targets |> List.iter (fun tgt -> tgt.Complete ())) |> ignore
            let split = ActionBlock(fun t ->
                    targets |> List.iter (fun tgt -> tgt.Post (t) |> ignore) )
            let link = buffer.LinkTo (split, DataflowLinkOptions(PropagateCompletion = true))
            buffer, leaves, link :: disposables
