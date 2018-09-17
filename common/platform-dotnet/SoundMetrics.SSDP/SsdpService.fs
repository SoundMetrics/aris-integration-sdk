// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open SsdpMessages
open System
open System.Threading.Tasks.Dataflow

type ServiceTypeInfo = {
    InfoLocation        : string
    Server              : string
    UniqueServerName    : string
}

type ISsdpServiceCallbacks =
    abstract member IsAlive : bool
    /// Implementations MUST return a ServiceTypeInfo object, not null.
    abstract member GetServiceTypeInfo : string -> ServiceTypeInfo array

module internal SsdpServiceDetails =

    let buildNotifyMsg (stInfo : ServiceTypeInfo) =

        failwith "nyi"

    let handleIncomingMessage (supportedServices : Set<string>) (callbacks : ISsdpServiceCallbacks) queueOutgoing msgAndTraits =

        if callbacks.IsAlive then
            let _, msg = msgAndTraits
            match msg with
            | MSearch msg when supportedServices |> Set.contains msg.ST ->
                let serviceInfos = callbacks.GetServiceTypeInfo(msg.ST)
                if isNull (box serviceInfos) then
                    failwith "GetServiceTypeInfo must not return null"

                serviceInfos
                |> Array.iter (fun serviceInfo ->
                    serviceInfo
                    |> buildNotifyMsg
                    |> SsdpMessage.ToPacket
                    |> queueOutgoing)

            | MSearch _ -> () // Discovery request service type is not supported by this service.
            | Notify _ -> failwith "not implemented"
            | Unhandled _ -> ()
        else
            () // If the service doesn't claim to be alive, we don't send anything out.

    let sendSsdpMessage _msg =

        failwith "nyi"


open SsdpServiceDetails

type SsdpService (supportedServiceTypes : string seq, callbacks : ISsdpServiceCallbacks) =

    let mutable disposed = false
    let ssdpClient = new SsdpClient()
    let outgoingMessageQueue = new BufferBlock<_>()
    let serviceTypes = Set supportedServiceTypes

    let postOutgoing pkt = outgoingMessageQueue.Post(pkt) |> ignore

    let messageSub =
        // Partial application here
        let handleIncoming = handleIncomingMessage serviceTypes callbacks postOutgoing
        ssdpClient.MessageSourceBlock.LinkTo(ActionBlock<_>(handleIncoming))
    let outgoingSub = outgoingMessageQueue.LinkTo(ActionBlock<_>(sendSsdpMessage))

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpService"))

            disposed <- true

            // Clean up managed resources
            messageSub.Dispose()
            outgoingSub.Dispose()
            ssdpClient.Dispose()

        // Clean up native resources
        ()

    do
        if isNull supportedServiceTypes then
            invalidArg "supportedServiceTypes" "Must not be null"
        if isNull (box callbacks) then
            invalidArg "callbacks" "Must not be null"

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false
