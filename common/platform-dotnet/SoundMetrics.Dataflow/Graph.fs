namespace SoundMetrics.Dataflow

open System
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

module Graph =

    // This has right associativity so, more or less, the graph binds from
    // right to left. Pointing the arrow right may be less correct in that
    // respect, but it is more readable when you're looking at a graph.
    let inline (^|>) f a = f a

    let inline fst (item, _, _) = item
    let inline snd (_, item, _) = item
    let inline thd (_, _, item) = item

    type RH<'T> = ITargetBlock<'T> * Task list * IDisposable list

    let dfbOptions = DataflowLinkOptions(PropagateCompletion = true)

    let leaf<'T> (f : 'T -> unit) =
        let block = ActionBlock<'T>(f)
        block :> ITargetBlock<'T>, [block.Completion], []

    let buffer<'T> boundedCapacity (rh : ITargetBlock<'T>, leaves, disposables) =

        let buffer =  if boundedCapacity > 0 then
                        let options = DataflowBlockOptions(BoundedCapacity = boundedCapacity)
                        BufferBlock<'T>(options)
                      else
                        BufferBlock<'T>()

        let d = buffer.LinkTo(rh, dfbOptions)
        buffer :> ITargetBlock<'T>, leaves, d :: disposables

    let tee<'T> (rhs : Tuple<ITargetBlock<'T>, Task list, IDisposable list> list) =

        let cached = rhs |> Seq.cache
        let targets : ITargetBlock<'T> seq = cached |> Seq.map fst |> Seq.cache
        let allLeaves = cached |> Seq.map snd |> List.concat
        let allDisposables : (IDisposable list) = cached |> Seq.map thd |> List.concat
        let action = ActionBlock<'T>(fun t ->
                        targets |> Seq.iter (fun target -> target.Post(t) |> ignore))

        action.Completion.ContinueWith(fun _ ->
                targets |> Seq.iter (fun target -> target.Complete())
            )
            |> ignore

        action :> ITargetBlock<'T>, allLeaves, allDisposables

    let transform<'T,'U> (f : 'T -> 'U) (rhs : ITargetBlock<'U>, leaves, disposables) =

        let tf = TransformBlock<'T,'U>(f)
        let d = tf.LinkTo(rhs, dfbOptions)
        tf :> ITargetBlock<'T>, leaves, d :: disposables

    let filter<'T> (predicate : 'T -> bool) (rh : ITargetBlock<'T>, leaves, disposables) =

        let action = ActionBlock<'T>(fun t ->
                        if predicate t then
                            rh.Post(t) |> ignore)

        action.Completion.ContinueWith(fun _ -> rh.Complete()) |> ignore

        action :> ITargetBlock<'T>, leaves, disposables

    type GraphHandle<'T> (root : RH<'T>) =

        let mutable disposed = false
        let mutable completing = false
        let root, leaves, disposables = root

        let completeAndWait (timeout : TimeSpan) =
            completing <- true
            root.Complete()
            Task.WaitAll(leaves |> Seq.toArray, timeout)

        let dispose disposing =

            if disposed then
                raise (ObjectDisposedException "GraphHandle")

            if disposing then
                disposed <- true

                if not completing then
                    completeAndWait (TimeSpan.FromSeconds(1.0)) |> ignore

                // Clean up managed resources
                disposables |> List.iter (fun d -> d.Dispose())

            // Clean up unmanaged resources
            ()

        interface IDisposable with
            member me.Dispose() = dispose true
                                  GC.SuppressFinalize(me)

        member me.Dispose() = (me :> IDisposable).Dispose()
        override __.Finalize() = dispose false

        member __.Post(t : 'T) : bool =

            if disposed then
                raise (ObjectDisposedException "GraphHandle")

            if not completing then
                root.Post(t)
            else
                false

        member __.CompleteAndWait(timeout : TimeSpan) : bool =

            completeAndWait timeout
