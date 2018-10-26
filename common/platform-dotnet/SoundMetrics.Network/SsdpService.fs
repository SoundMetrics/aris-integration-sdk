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
                           sendPacket
                           (recvd : SsdpMessageReceived) =

        match recvd.Message with
        | MSearch msg when serviceInfoMap |> Map.containsKey msg.ST ->
            let serviceInfo = serviceInfoMap.[msg.ST]
            if serviceInfo.ServiceInfo.IsActive() then
                serviceInfo |> buildNotifyAliveMsg debugLogging recvd.Properties.LocalEndPoint.Address
                            |> Option.iter (fun aliveMsg ->
                                aliveMsg |> SsdpMessage.ToPacket
                                         |> sendPacket recvd.Properties.RemoteEndPoint
                            )

        | MSearch _ ->  () // Discovery request service type is not supported by this service.
        | Notify _ ->   () // Ignoring notify messages.
        | Response _ -> () // Ignoring response messages; these should happen.
        | Unhandled _ ->() // Unhandled, by definition.

    let sendUnsolicitedAlive debugLogging (serviceInfoMap : Map<string, ServicePrivateInfo>) =

        for kvp in serviceInfoMap do
            let priv = kvp.Value

            for (_location, listener) in priv.Listeners do
                let addr = (listener.LocalEndpoint :?> IPEndPoint).Address

                match buildNotifyAliveMsg debugLogging addr priv with
                | Some aliveMsg ->
                    use udp = new UdpClient(IPEndPoint(addr, 0))
                    let sendPacket ep (buffer : byte array) =
                        udp.Send(buffer, buffer.Length, ep) |> ignore
                    aliveMsg |> SsdpMessage.ToPacket
                             |> sendPacket SsdpConstants.SsdpEndPointIPv4
                | None -> ()
        

    let buildInfoRequest (info : ServicePrivateInfo) =

        let body = info.ServiceInfo.GetServiceBodyText()
        if isNull body then
            raise (ArgumentNullException("(return)", "GetServiceBodyText must not return null"))

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

        while tcp.Client.Available > 0 do
            tcp.Client.Receive(buf) |> ignore // Don't pay any attention to the contents, we'll respond regardless.

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

    type ServiceAction =
        | InfoRequest of SsdpMessageReceived
        | PeriodicAliveMessage
        | SendAlive
        | Drain of (unit -> unit)

    let mkPeriodicAction (period : TimeSpan) action =

        let timer =
            let state = None
            new System.Threading.Timer((fun _ -> action()), state, period, period)
        timer :> IDisposable


open SsdpInfoServing
open SsdpServiceDetails
open Serilog.Events
open System.Threading

type SsdpService (name : string,
                  supportedServiceTypes : SsdpServiceInfo seq,
                  aliveAnnouncementPeriod : TimeSpan,
                  multicastLoopback : bool,
                  debugLogging : bool) =

    let mutable disposed = false
    let ssdpClient =
        let clientName = sprintf "SsdpService[%s].Client" name
        new SsdpClient(clientName, multicastLoopback, debugLogging)

    let svcMap =
        let toKvp svc = svc.ServiceInfo.ServiceType, svc
        buildServiceInfos supportedServiceTypes
                    |> Seq.map toKvp
                    |> Map.ofSeq

    let actionQueue = new BufferBlock<_>()

    let actionHandler = new ActionBlock<_>(fun action ->
            let sendPacket (localAddr : IPAddress) (ep : IPEndPoint) (buffer : byte array) =
                use udp = new UdpClient(IPEndPoint(localAddr, 0))
                if ep = SsdpConstants.SsdpEndPointIPv4 then
                    udp.JoinMulticastGroup(SsdpConstants.SsdpEndPointIPv4.Address)
                    udp.MulticastLoopback <- true

                Log.Information("SsdpService: sending {length} bytes to {ep}", buffer.Length, ep)
                if not (udp.Send(buffer, buffer.Length, ep) = buffer.Length) then
                    Log.Warning("SsdpService: send package failed")

            match action with
            | InfoRequest recvd ->
                Async.Start(async {
                    do! Async.Sleep(100) // delay a bit before responding, per protocol description
                    let localAddr =
                        NetworkSupport.findLocalIPAddress recvd.Properties.RemoteEndPoint.Address IPAddress.Any

                    handleIncomingSsdp debugLogging svcMap (sendPacket localAddr) recvd
                })

            | SendAlive | PeriodicAliveMessage ->
                sendUnsolicitedAlive debugLogging svcMap
            | Drain notify -> notify()
        )
    let actionSub = actionQueue.LinkTo(actionHandler)

    let messageSub =
        let queueRequest msg = actionQueue.Post (InfoRequest msg) |> ignore
        ssdpClient.Messages.LinkTo(ActionBlock<_>(queueRequest))

    let periodicAliveAction = mkPeriodicAction aliveAnnouncementPeriod
                                               (fun () -> actionQueue.Post PeriodicAliveMessage |> ignore)

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpService"))

            Log.Debug("Disposing SsdpService.")
            disposed <- true

            // Clean up managed resources

            periodicAliveAction.Dispose()

            svcMap |> Seq.map (fun kvp -> kvp.Value) |> cleanUpInfoListeners

            // Drop the incoming link before draining the buffer.
            messageSub.Dispose()
            ssdpClient.Dispose()

            use isDrained = new ManualResetEventSlim()
            actionQueue.Post(Drain (fun () -> isDrained.Set())) |> ignore
            if not (isDrained.Wait(TimeSpan.FromSeconds(0.5))) then
                Log.Warning("SsdpService: timed out waiting to drain action queue")

            actionSub.Dispose()

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
        actionQueue.Post SendAlive |> ignore

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false

    member __.Name = name
