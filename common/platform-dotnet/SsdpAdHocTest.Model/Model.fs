namespace SsdpAdHocTest.Model

open SoundMetrics.Common
open System.Collections.ObjectModel
open System
open System.Threading

type AmbientMessage = {
    Content : string
}

type TheModel (syncCtx : SynchronizationContext) =

    let mutable disposed = false
    let ambientMessages = ObservableCollection<string>()
    let client = new SsdpClient()

    let onReceive msg =
        let callback = fun _ ->
            if ambientMessages.Count >= 100 then
                ambientMessages.RemoveAt(ambientMessages.Count - 1)
        
            ambientMessages.Insert(0, msg)
        syncCtx.Post(SendOrPostCallback callback, ())

    let clientSub = client.Messages.Subscribe(onReceive)

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException "TheModel")

            disposed <- true

            // Clean up managed resources
            clientSub.Dispose()
            client.Dispose()

        // Clean up native resources
        ()

    interface IDisposable with
        override me.Dispose () =    dispose true
                                    GC.SuppressFinalize me
    member me.Dispose () = (me :> IDisposable).Dispose()
    override __.Finalize () = dispose false

    member __.AmbientMessages = ambientMessages
