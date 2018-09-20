// Copyright 2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

open Serilog
open SsdpMessages
open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading.Tasks.Dataflow

type SsdpServiceInfo = {
    /// The service type name of the service.
    ServiceType         : string

    /// A descriptive name of the service.
    Server              : string

    /// The unique name identifying this specific service.
    UniqueServerName    : string

    /// The MIME type used for Location requests. E.g., `text/x.nvp`.
    /// (The HTTP interaction is managed by SsdpService.)
    MimeType            : string

    /// Callback to determine if the serivce is active.
    IsActive            : unit -> bool

    /// Callback to retrieve body text for Location requests
    /// (the HTTP interaction is managed by SsdpService).
    GetServiceBodyText  : unit -> string
}

module internal SsdpServiceDetails =

    [<Literal>]
    let CRLF = "\r\n"

    type Location = string

    type ServicePrivateInfo = {
        ServiceInfo : SsdpServiceInfo
        Path        : string
        Listeners   : (Location * TcpListener) list
    }

    let buildNotifyAliveMsg (ifcAddr : IPAddress) (info : ServicePrivateInfo) =

        let matchingListener =
            info.Listeners |> Seq.filter (fun (_, listener) ->
                                            let ep = listener.LocalEndpoint :?> IPEndPoint
                                            ifcAddr = ep.Address)
                           |> Seq.cache
        if matchingListener |> Seq.isEmpty then
            Log.Information("Couldn't find listener for interface address {addr}", ifcAddr)
            None
        else
            let si = info.ServiceInfo
            let location, _listener = matchingListener |> Seq.head
            Some
                (Notify
                    {
                        Host = SsdpConstants.SsdpEndPointIPv4
                        Location = location
                        ST = si.ServiceType
                        Server = si.Server
                        USN = si.UniqueServerName
                        CacheControl = Empty
                        NTS = "\"ssdp:alive\""
                    })

    let handleIncomingSsdp (serviceInfoMap : Map<string, ServicePrivateInfo>)
                            queueOutgoing
                            ((msgProps, msg) : SsdpMessageProperties * SsdpMessage) =

        match msg with
        | MSearch msg when serviceInfoMap |> Map.containsKey msg.ST ->
            let serviceInfo = serviceInfoMap.[msg.ST]
            if serviceInfo.ServiceInfo.IsActive() then
                serviceInfo |> buildNotifyAliveMsg msgProps.LocalEndPoint.Address
                            |> Option.iter (fun aliveMsg ->
                                aliveMsg |> SsdpMessage.ToPacket
                                         |> queueOutgoing
                            )

        | MSearch _ -> () // Discovery request service type is not supported by this service.
        | Notify _ ->   failwith "not implemented"
        | Response _ -> failwith "not implemented"
        | Unhandled _ -> ()

    let buildInfoRequest (info : ServicePrivateInfo) =

        let body = info.ServiceInfo.GetServiceBodyText()
        let header =    "HTTP/1.1 200 OK" + CRLF
                        + (sprintf "CONTENT-LENGTH: %d" (Encoding.ASCII.GetByteCount body)) + CRLF
                        + (sprintf "CONTENT-TYPE: %s" info.ServiceInfo.MimeType) + CRLF
                        + (sprintf "DATE: %s" (DateTimeOffset.Now.ToString("r"))) + CRLF
                        + CRLF

        struct (Encoding.ASCII.GetBytes(header),
                Encoding.ASCII.GetBytes(body))

module internal SsdpInfoServing =
    open SoundMetrics.Network.SsdpNetworkInterfaces
    open SsdpServiceDetails

    let drainClientHeader (tcp : TcpClient) =

        let buf = Array.zeroCreate<byte> 1000
        let mutable cb = 0
        while (cb = tcp.Client.Receive(buf)) && cb <> 0 do
            printfn "### %s" (Encoding.ASCII.GetString(buf, 0, cb)) // TODO REMOVE
            () // Don't pay any attention to the contents, we'll respond regardless. TODO ??

    let sendInfoRequest (tcp : TcpClient) (info : ServicePrivateInfo) =

        let struct (header, body) = buildInfoRequest info
        tcp.Client.Send([| ArraySegment(header); ArraySegment(body) |]) |> ignore

    let processClientConnection (tcp : TcpClient) (info : ServicePrivateInfo) =

        drainClientHeader tcp
        sendInfoRequest tcp info

    let rec beginAcceptClientConnection (listener : TcpListener) (svc : ServicePrivateInfo) =

        listener.BeginAcceptTcpClient(AsyncCallback onAcceptClientConnection,
                                      (listener, svc)) |> ignore

    and onAcceptClientConnection (iar : IAsyncResult) =

        let listener, svc = iar.AsyncState :?> (TcpListener * ServicePrivateInfo)
        if iar.IsCompleted then
            let client = listener.EndAcceptTcpClient(iar)
            processClientConnection client svc
            beginAcceptClientConnection listener svc

    let createInfoListeners (ifcs : Interface seq) =

        ifcs |> Seq.map (fun ifc -> new TcpListener(ifc.Address, 0))

    let buildServiceInfos (serviceTypes : SsdpServiceInfo seq) =

        let ifcs = getSspdInterfaces()

        serviceTypes |> Seq.map (fun svc ->
                        let listeners =
                            createInfoListeners ifcs 
                                |> Seq.map (fun listener ->
                                    let location =
                                        let ep = listener.LocalEndpoint :?> IPEndPoint
                                        UriBuilder("http", ep.Address.ToString(), ep.Port,
                                                   svc.ServiceType).ToString()
                                    (location, listener)
                                )
                                |> Seq.toList

                        {   ServiceInfo = svc
                            Path = svc.ServiceType
                            Listeners = listeners })

    let initInfoListeners (svcs : ServicePrivateInfo seq) =

        for svc in svcs do
            for (_loc, listener) in svc.Listeners do
                listener.Start()
                beginAcceptClientConnection listener svc

    let cleanUpInfoListeners (svcs : ServicePrivateInfo seq) =

        for svc in svcs do
            for (_loc, listener) in svc.Listeners do
                listener.Stop()


open SsdpInfoServing
open SsdpServiceDetails

type SsdpService (supportedServiceTypes : SsdpServiceInfo seq) =

    let mutable disposed = false
    let ssdpClient = new SsdpClient()
    let outgoingMessageQueue = new BufferBlock<_>()
    let svcMap =
        let toKvp svc = svc.ServiceInfo.ServiceType, svc
        buildServiceInfos supportedServiceTypes
                    |> Seq.map toKvp
                    |> Map.ofSeq
    let outgoingSocket = new UdpClient()

    let sendSsdpMessage packet =
        outgoingSocket.Send(packet, packet.Length, SsdpConstants.SsdpEndPointIPv4) |> ignore

    let postOutgoing pkt = outgoingMessageQueue.Post(pkt) |> ignore

    let messageSub =
        // Partial application here
        let handleIncoming = handleIncomingSsdp svcMap postOutgoing
        ssdpClient.MessageSourceBlock.LinkTo(ActionBlock<_>(handleIncoming))
    let outgoingSub = outgoingMessageQueue.LinkTo(ActionBlock<_>(sendSsdpMessage))

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpService"))

            disposed <- true

            // Clean up managed resources
            svcMap |> Seq.map (fun kvp -> kvp.Value) |> cleanUpInfoListeners

            messageSub.Dispose()
            outgoingSub.Dispose()
            ssdpClient.Dispose()
            outgoingSocket.Dispose()

        // Clean up native resources
        ()

    do
        if isNull supportedServiceTypes then
            invalidArg "supportedServiceTypes" "Must not be null"
        if svcMap.Count = 0 then
            invalidArg "supportedServiceTypes" "Must not be empty"

        svcMap |> Seq.map (fun kvp -> kvp.Value) |> initInfoListeners

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false
