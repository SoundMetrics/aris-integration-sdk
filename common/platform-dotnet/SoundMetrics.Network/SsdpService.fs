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
    open Serilog.Events

    [<Literal>]
    let CRLF = "\r\n"

    type Location = string

    type ServicePrivateInfo = {
        ServiceInfo : SsdpServiceInfo
        Path        : string
        Listeners   : (Location * TcpListener) list
    }

    let buildNotifyAliveMsg debugLogging
                            (ifcAddr : IPAddress)
                            (info : ServicePrivateInfo) =

        let matchingListener =
            info.Listeners |> Seq.filter (fun (_, listener) ->
                                            let ep = listener.LocalEndpoint :?> IPEndPoint
                                            ifcAddr = ep.Address)
                           |> Seq.cache
        if matchingListener |> Seq.isEmpty then
            Log.Warning("Couldn't find listener for interface address {addr}", ifcAddr)
            None
        else
            let si = info.ServiceInfo

            if debugLogging && Log.IsEnabled(LogEventLevel.Debug) then
                Log.Debug("buildNotifyAliveMsg: build message for {usn}", si.UniqueServerName)

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

    let handleIncomingSsdp debugLogging
                           (serviceInfoMap : Map<string, ServicePrivateInfo>)
                           queueOutgoing
                           ((msgProps, msg) : SsdpMessageProperties * SsdpMessage) =

        match msg with
        | MSearch msg when serviceInfoMap |> Map.containsKey msg.ST ->
            let serviceInfo = serviceInfoMap.[msg.ST]
            if serviceInfo.ServiceInfo.IsActive() then
                serviceInfo |> buildNotifyAliveMsg debugLogging msgProps.LocalEndPoint.Address
                            |> Option.iter (fun aliveMsg ->
                                aliveMsg |> SsdpMessage.ToPacket
                                         |> queueOutgoing msgProps.RemoteEndPoint
                            )

        | MSearch _ ->  () // Discovery request service type is not supported by this service.
        | Notify _ ->   () // Ignoring notify messages.
        | Response _ -> () // Ignoring response messages; these should happen.
        | Unhandled _ ->() // Unhandled, by definition.

    let sendUnsolicitedAlive debugLogging queueOutgoing (serviceInfoMap : Map<string, ServicePrivateInfo>) =

        for kvp in serviceInfoMap do
            let priv = kvp.Value

            for (_location, listener) in priv.Listeners do
                let addr = (listener.LocalEndpoint :?> IPEndPoint).Address

                match buildNotifyAliveMsg debugLogging addr priv with
                | Some aliveMsg ->
                    aliveMsg |> SsdpMessage.ToPacket
                             |> queueOutgoing SsdpConstants.SsdpEndPointIPv4
                | None -> ()
        

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
        let read () =
            let cb = tcp.Client.Receive(buf)
            cb <> 0

        while read() do
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

        ifcs |> Seq.map (fun ifc -> let listener = new TcpListener(ifc.Address, 0)
                                    // Need to start the listener now so we get a port number
                                    // below for buildServiceInfos.
                                    listener.Start()
                                    listener)

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
                beginAcceptClientConnection listener svc

    let cleanUpInfoListeners (svcs : ServicePrivateInfo seq) =

        for svc in svcs do
            for (_loc, listener) in svc.Listeners do
                listener.Stop()


open SsdpInfoServing
open SsdpServiceDetails
open Serilog.Events

type SsdpService (name : string,
                  supportedServiceTypes : SsdpServiceInfo seq,
                  multicastLoopback : bool,
                  debugLogging : bool) =

    let mutable disposed = false
    let ssdpClient =
        let clientName = sprintf "SsdpService[%s].Client" name
        new SsdpClient(clientName, multicastLoopback)
    let outgoingMessageQueue = new BufferBlock<_>()
    let svcMap =
        let toKvp svc = svc.ServiceInfo.ServiceType, svc
        buildServiceInfos supportedServiceTypes
                    |> Seq.map toKvp
                    |> Map.ofSeq
    let outgoingSocket = new UdpClient()

    let sendSsdpMessage (ep, (packet : byte array)) =
        Async.Start(async {
            do! Async.Sleep(100) // delay a bit before responding

            let packetSize = packet.Length
            if debugLogging && Log.IsEnabled(LogEventLevel.Debug) then
                Log.Debug("sendSsdpMessage: sending {length}-byte packet to {destination}", packetSize, ep)

            let lengthSent = outgoingSocket.Send(packet, packet.Length, ep)
            if lengthSent <> packetSize then
                Log.Warning("sendSsdpMessage failed: {lengthSent} of {toSend} bytes sent",
                            lengthSent, packetSize)
        })

    let postOutgoing ep pkt = outgoingMessageQueue.Post(ep, pkt) |> ignore

    let messageSub =
        // Partial application here
        let handleIncoming = handleIncomingSsdp debugLogging svcMap postOutgoing
        ssdpClient.Messages.LinkTo(ActionBlock<_>(handleIncoming))
    let outgoingSub = outgoingMessageQueue.LinkTo(ActionBlock<_>(sendSsdpMessage))

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpService"))

            Log.Debug("Disposing SsdpService.")
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

        if Log.IsEnabled(LogEventLevel.Debug) then
            Log.Debug("SsdpService.ctor: {name}; {serviceCount} service(s)", name, svcMap.Count)
            for kvp in svcMap do
                let info = sprintf "%A" kvp.Value
                Log.Debug("SsdpService.ctor:   {svcName} {info}", kvp.Key, info)

        svcMap |> Seq.map (fun kvp -> kvp.Value) |> initInfoListeners

        Log.Debug("SsdpService.ctor: sending initial alive message")
        sendUnsolicitedAlive debugLogging postOutgoing svcMap

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false

    member __.Name = name
