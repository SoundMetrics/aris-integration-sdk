namespace SsdpAdHocTest.Model

open SoundMetrics.Network
open SoundMetrics.Network.SsdpMessages
open System.Collections.ObjectModel
open System
open System.Threading
open System.Threading.Tasks.Dataflow

type AmbientMessage = {
    Content : string
}

type TheModel (syncCtx : SynchronizationContext) as self =

    inherit fracas.NotifyBase ()

    let mutable disposed = false
    let ambientMessages = ObservableCollection<string>()
    let client = new SsdpClient("TheModel.Client", multicastLoopback = true)
    let selfServiceType = "SsdpAdHocTest.Model.MyService"
    let _ssdpService = MyService.buildSsdpService selfServiceType ("SsdpAdHocTestWPF-" + Guid.NewGuid().ToString())
                            "this is a service, this is only a service"
                            true // multicast loopback

    let isAmbientEnabled =  self |> fracas.mkField <@ self.IsAmbientEnabled @>  true
    let isNotifyOnly =      self |> fracas.mkField <@ self.IsNotifyOnly @>      true
    let isSendEnabled =     self |> fracas.mkField <@ self.IsSendEnabled @>     true
    let isSelfEnabled =     self |> fracas.mkField <@ self.IsSelfEnabled @>     true

    let onReceive (recvd : SsdpMessages.SsdpMessageReceived) =
        let showMsg =
            isAmbientEnabled.Value &&
                match recvd.Message, isNotifyOnly.Value with
                | Notify _, _ -> true
                | Response _, _ -> true
                | _, nOnly -> not nOnly

        if showMsg then
            let callback = fun _ ->
                if ambientMessages.Count >= 100 then
                    ambientMessages.RemoveAt(ambientMessages.Count - 1)
        
                let s = recvd.Properties.RawContent
                ambientMessages.Insert(0, s)
            syncCtx.Post(SendOrPostCallback callback, ())

    let onReceiveAction = ActionBlock<_>(onReceive)

    let searchKnownGood _ =
            let service = "urn:schemas-upnp-org:service:Power:1"
            let ua = "SsdpAdHocTestWPF"
            isSendEnabled.Value <- false
            searchAsync(service, ua, TimeSpan.FromSeconds(5.0), false, onReceive)

    let searchKnownGoodCommand =
        let cleanUp _ = isSendEnabled.Value <- true
        self |> fracas.mkAsyncCommand (fun _ -> true)
                                      searchKnownGood
                                      cleanUp
                                      cleanUp
                                      cleanUp
                                      (fun () -> None)
                                      []

    let selfSearchProgress = ObservableCollection<string>()

    let searchSelf _ =

        isSelfEnabled.Value <- false // on the UI thread
        selfSearchProgress.Clear()
        selfSearchProgress.Add("Searching self...")

        let update =
            let uiCtx = SynchronizationContext.Current
            fun msg -> uiCtx.Post(SendOrPostCallback
                                        (fun _ -> selfSearchProgress.Add(msg)),
                                  ())
                        

        async {
            do! Async.Sleep(1000)
            let service = selfServiceType
            let ua = "SsdpAdHocTestWPF"
            update "Running search..."
            let onReceive' (recvd : SsdpMessageReceived) =
                update (sprintf "Got a message from %A:\n%s"
                    recvd.Properties.RemoteEndPoint recvd.Properties.RawContent)
                onReceive recvd
            return! searchAsync(service, ua, TimeSpan.FromSeconds(5.0), true, onReceive')
        }

    let searchSelfServiceCommand =
        let cleanUp _ = isSelfEnabled.Value <- true
        self |> fracas.mkAsyncCommand (fun _ -> true)
                                      searchSelf
                                      cleanUp
                                      cleanUp
                                      cleanUp
                                      (fun () -> None)
                                      []

    let clientLink = client.Messages.LinkTo(onReceiveAction)

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException "TheModel")

            disposed <- true

            // Clean up managed resources
            clientLink.Dispose()
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
    member __.IsSelfEnabled = isSelfEnabled.Value

    member __.SearchKnownGoodCommand = searchKnownGoodCommand.ICommand
    member __.SearchSelfServiceCommand = searchSelfServiceCommand.ICommand

    member __.SelfSearchProgress = selfSearchProgress
