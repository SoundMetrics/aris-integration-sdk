// Copyright 2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

open SsdpMessages
open System
open System.Net.Sockets
open System.Threading.Tasks.Dataflow

type SsdpServiceTypeInfo = {
    /// The service type name of the service.
    ServiceType         : string
    /// A descriptive name of the service.
    Server              : string
    /// The unique name identifying this specific service.
    UniqueServerName    : string
    /// The location of more information to be retrieved.
    InfoLocation        : string
}

type ISsdpServiceCallbacks =
    abstract member IsAlive : bool
    /// Implementations MUST return a ServiceTypeInfo object, not null.
    abstract member GetServiceTypeInfo : string -> SsdpServiceTypeInfo array

module internal SsdpServiceDetails =

    let buildNotifyAliveMsg (stInfo : SsdpServiceTypeInfo) =
        Notify
            {
                Host = SsdpConstants.SsdpEndPointIPv4
                Location = stInfo.InfoLocation
                ST = stInfo.ServiceType
                Server = stInfo.Server
                USN = stInfo.UniqueServerName
                CacheControl = Empty
                NTS = "ssdp:alive"
            }

    let handleIncomingMessage (supportedServices : Set<string>) (callbacks : ISsdpServiceCallbacks) queueOutgoing msgAndTraits =

        if callbacks.IsAlive then
            let _, msg = msgAndTraits
            match msg with
            | MSearch msg when supportedServices |> Set.contains msg.NT ->
                let serviceInfos = callbacks.GetServiceTypeInfo(msg.NT)
                if isNull (box serviceInfos) then
                    failwith "GetServiceTypeInfo must not return null"

                serviceInfos
                |> Array.iter (fun serviceInfo ->
                    serviceInfo
                    |> buildNotifyAliveMsg
                    |> SsdpMessage.ToPacket
                    |> queueOutgoing)

            | MSearch _ -> () // Discovery request service type is not supported by this service.
            | Notify _ -> failwith "not implemented"
            | Unhandled _ -> ()
        else
            () // If the service doesn't claim to be alive, we don't send anything out.


open SsdpServiceDetails

type SsdpService (supportedServiceTypes : string seq, callbacks : ISsdpServiceCallbacks) =

    let mutable disposed = false
    let ssdpClient = new SsdpClient()
    let outgoingMessageQueue = new BufferBlock<_>()
    let serviceTypes = Set supportedServiceTypes
    let outgoingSocket = new UdpClient()

    let sendSsdpMessage packet =
        outgoingSocket.Send(packet, packet.Length, SsdpConstants.SsdpEndPointIPv4) |> ignore

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
            outgoingSocket.Dispose()

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
