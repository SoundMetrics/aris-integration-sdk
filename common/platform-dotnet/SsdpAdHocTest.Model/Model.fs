namespace SsdpAdHocTest.Model

open SoundMetrics.Network
open SoundMetrics.Network.SsdpMessages
open System.Collections.ObjectModel
open System
open System.Threading

type AmbientMessage = {
    Content : string
}

type TheModel (syncCtx : SynchronizationContext) as self =

    inherit fracas.NotifyBase ()

    let mutable disposed = false
    let ambientMessages = ObservableCollection<string>()
    let client = new SsdpClient()

    let isAmbientEnabled =  self |> fracas.mkField <@ self.IsAmbientEnabled @>  true
    let isNotifyOnly =      self |> fracas.mkField <@ self.IsNotifyOnly @>      true
    let isSendEnabled =     self |> fracas.mkField <@ self.IsSendEnabled @>     true

    let onReceive (props : SsdpMessages.SsdpMessageProperties, msg) =
        let showMsg =
            isAmbientEnabled.Value &&
                match msg, isNotifyOnly.Value with
                | Notify _, _ -> true
                | Response _, _ -> true
                | _, nOnly -> not nOnly

        if showMsg then
            let callback = fun _ ->
                if ambientMessages.Count >= 100 then
                    ambientMessages.RemoveAt(ambientMessages.Count - 1)
        
                let s = props.RawContent
                ambientMessages.Insert(0, s)
            syncCtx.Post(SendOrPostCallback callback, ())

    let searchKnownGood _ =
            let service = "urn:schemas-upnp-org:service:Power:1"
            let ua = "SsdpAdHocTestWPF"
            isSendEnabled.Value <- false
            SsdpClient.SearchAsync(service, ua, TimeSpan.FromSeconds(5.0), onReceive)

    let searchKnownGoodCommand =
        let cleanUp _ = isSendEnabled.Value <- true
        self |> fracas.mkAsyncCommand (fun _ -> true)
                                      searchKnownGood
                                      cleanUp
                                      cleanUp
                                      cleanUp
                                      (fun () -> None)
                                      []

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

    member __.IsAmbientEnabled
        with get () = isAmbientEnabled.Value
        and set newValue = isAmbientEnabled.Value <- newValue
    member __.IsNotifyOnly
        with get () = isNotifyOnly.Value
        and set newValue = isNotifyOnly.Value <- newValue
    member __.IsSendEnabled = isSendEnabled.Value

    member __.SearchKnownGoodCommand = searchKnownGoodCommand.ICommand
