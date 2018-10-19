// Copyright 2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

open Serilog
open System
open System.Net
open System.Net.NetworkInformation
open System.Threading

(*

    This assembly contains a minimal implementation of Simple Service Discovery Protocol
    (SSDP). This is based on IETF RFC https://tools.ietf.org/pdf/draft-cai-ssdp-v1-03.pdf
    which expired in 2000, but is the basis for UPnP.

    See also: SsdpClient, SsdpService.

    The SSDP code lives in this assembly in order to be co-located with BeaconListener.

*)

open SsdpMessages

module internal SsdpNetworkInterfaces =
    open System.Net.Sockets

    /// Fetches the NICs of interest for SSDP.
    let private getSsdpNics () =

        NetworkInterface.GetAllNetworkInterfaces()
            |> Seq.filter (fun nic ->
                    nic.OperationalStatus = OperationalStatus.Up
                    && nic.Supports(NetworkInterfaceComponent.IPv4)
                    && nic.NetworkInterfaceType <> NetworkInterfaceType.Loopback)
            |> Seq.toArray

    let private allowedAddressFamilies =
        [ AddressFamily.InterNetwork // IPv4
          AddressFamily.InterNetworkV6 ]
        |> Set

    let private selectUnicastAddress (nic : NetworkInterface) =

        // Prefer IPv4, allow IPv6
        let isAllowedFamily (af, _) = allowedAddressFamilies.Contains af
        let toSortRank (af, _) = match af with
                                 | AddressFamily.InterNetwork -> 0
                                 | AddressFamily.InterNetworkV6 -> 1
                                 | _ -> failwithf "Unexpected AddressFamily: %A" af

        let addrs = nic.GetIPProperties().UnicastAddresses
                        |> Seq.map (fun u -> u.Address.AddressFamily, u.Address)
                        |> Seq.filter isAllowedFamily
                        |> Seq.sortBy toSortRank
                        |> Seq.cache
        if addrs |> Seq.isEmpty then
            None
        else
            Some (addrs |> Seq.head |> snd)

    /// A network interface.
    type Interface = {
        Id      : string
        Name    : string
        Address : IPAddress
    }
    with
        static member FromNetworkInterface(nic : NetworkInterface) =
            selectUnicastAddress nic
                |> Option.map (fun addr ->
                    { Id = nic.Id
                      Name = nic.Description
                      Address = addr })

    /// Fetches the NICs of interest for SSDP.
    let getSspdInterfaces () =

        getSsdpNics()
            |> Seq.map Interface.FromNetworkInterface
            |> Seq.choose id // drop Nones

    /// Fetches the addresses of NICs of interest for SSDP.
    let getSspdAddresses () =

        getSspdInterfaces() |> Seq.map (fun ifc -> ifc.Address)


module internal SsdpInterfaceInputs =
    open SsdpConstants
    open SsdpNetworkInterfaces
    open System.Threading.Tasks.Dataflow
    open System.Net.Sockets

    // It would be nice if NetworkAddressChanged returned some useful information, but alas...
    // so we just have a catch-all input that triggers some contemplation of what interfaces
    // are still available. Then, to serialize operations in a thread-safe manner, we push
    // packets in through the same buffer block.
    type SsdpInterfaceInputs = NetworkChanged | Packet of MsgReceived

    /// Listens for packets on a given IP address. Packets are posted to `target`.
    type InterfaceListener (addr : IPAddress,
                            target : ITargetBlock<SsdpInterfaceInputs>,
                            multicastLoopback : bool,
                            debugLogging : bool) =

        let mutable disposed = false
        let cts = new CancellationTokenSource ()
        let doneSignal = new ManualResetEventSlim ()
        let udp = new UdpClient()

        let rec listen () =

            // Switched to task-based rather than wrapping in Async as sometimes the Async
            // just never completes. And that prevents the process from terminating.

            try
                let task = udp.ReceiveAsync()
                let action = Action<System.Threading.Tasks.Task<UdpReceiveResult>>(fun t ->
                    let now = DateTimeOffset.Now
                    let keepGoing = t.IsCompleted && not t.IsFaulted && not cts.IsCancellationRequested
                    if keepGoing then
                        let localEP = udp.Client.LocalEndPoint :?> IPEndPoint
                        let udpResult = task.Result
                        if debugLogging then
                            Log.Verbose("InterfaceListener: {localIP} received packet of {length} bytes from {remoteIP}", 
                                        localEP, udpResult.Buffer.Length, udpResult.RemoteEndPoint)
                        target.Post (Packet { UdpResult = udpResult; LocalEndPoint = localEP; Timestamp = now }) |> ignore
                        listen()
                    else
                        doneSignal.Set() )
                task.ContinueWith(action) |> ignore
            with
                :? ObjectDisposedException -> () // A chance of this when closing the socket.

        let dispose isDisposing =
            if isDisposing then
                if disposed then
                    raise (ObjectDisposedException "SsdpInterfaceInputs.InterfaceListener")

                // Clean up managed resources
                cts.Cancel ()
                udp.Close () // Because UdpClient doesn't know about cancellation--may make it throw.
                doneSignal.Wait ()

                let otherDisposables : IDisposable list = [udp; cts; doneSignal]
                otherDisposables |> List.iter (fun d -> if d <> null then d.Dispose())

            // Clean up native resources
            ()

        do
            udp.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
            udp.Client.Bind(IPEndPoint(addr, SsdpEndPointIPv4.Port)) // TODO .Any on right ifc
            udp.JoinMulticastGroup(SsdpEndPointIPv4.Address, addr)
            udp.MulticastLoopback <- multicastLoopback
            listen()

        interface IDisposable with
            override me.Dispose() = dispose true
                                    GC.SuppressFinalize me
        member me.Dispose() = (me :> IDisposable).Dispose()
        override __.Finalize() = dispose false

    /// Listens for SSDP messages on multiple NICs. Messages are published on `Messages`.
    type MultiInterfaceListener (multicastLoopback : bool, debugLogging : bool) as self =

        let mutable disposed = false
        let mutable interfaceMap = Map.empty<string, Interface>
        let mutable listenerMap = Map.empty<string, InterfaceListener>
        let inputBuffer = BufferBlock<_>()
        let outputBuffer = BufferBlock<SsdpMessageProperties * SsdpMessage>()

        let updateInterfaceMap () =

            let listener = sprintf "listener [%08X]: " (self.GetHashCode())
            Log.Information(listener + "A network change occurred.")

            let newNics =
                SsdpNetworkInterfaces.getSspdInterfaces()
                    |> Seq.map (fun ifc -> ifc.Id, ifc)
                    |> Map.ofSeq

            let expiredListeners =
                interfaceMap |> Seq.filter (fun kvp -> not (newNics.ContainsKey kvp.Key))
            for kvp in expiredListeners do
                Log.Information(listener + "  removing {name}; {address}", kvp.Value.Name, kvp.Value.Address)
                interfaceMap <- interfaceMap.Remove(kvp.Key)
                let old = listenerMap.[kvp.Key]
                old.Dispose()
                listenerMap <- listenerMap.Remove(kvp.Key)

            let newListeners =
                newNics |> Seq.filter (fun kvp -> not (interfaceMap.ContainsKey kvp.Key))
            for kvp in newListeners do
                Log.Information(listener + "  adding {name}; {address}", kvp.Value.Name, kvp.Value.Address)
                interfaceMap <- interfaceMap.Add(kvp.Key, kvp.Value)
                let listener = new InterfaceListener(kvp.Value.Address, inputBuffer, multicastLoopback, debugLogging)
                listenerMap <- listenerMap.Add(kvp.Key, listener)

        let handlePacket udpResult =

            let traits = SsdpMessageProperties.From(udpResult)
            match SsdpMessage.FromMulticast(udpResult) with
            | Ok msg -> outputBuffer.Post((traits, msg)) |> ignore
            | Error msg -> Log.Information("Bad message: {msg}", msg)

        let processInterfaceInput = function
            | NetworkChanged ->     updateInterfaceMap ()
            | Packet udpResult ->   handlePacket udpResult

        let processor = ActionBlock<_>(processInterfaceInput)

        let links = [
            inputBuffer.LinkTo(processor)
        ]

        let handleNetworkAddressChanged _ (e : EventArgs) =
            inputBuffer.Post NetworkChanged |> ignore

        let handleNetworkAvailabilityChanged _ (e : NetworkAvailabilityEventArgs) =
            inputBuffer.Post NetworkChanged |> ignore

        let addEventListeners () =
            NetworkChange.NetworkAddressChanged.AddHandler (NetworkAddressChangedEventHandler handleNetworkAddressChanged)
            NetworkChange.NetworkAvailabilityChanged.AddHandler (NetworkAvailabilityChangedEventHandler handleNetworkAvailabilityChanged)

        let removeEventListeners () =
            NetworkChange.NetworkAddressChanged.RemoveHandler (NetworkAddressChangedEventHandler handleNetworkAddressChanged)
            NetworkChange.NetworkAvailabilityChanged.RemoveHandler (NetworkAvailabilityChangedEventHandler handleNetworkAvailabilityChanged)

        let dispose isDisposing =
            if isDisposing then
                if disposed then
                    raise (ObjectDisposedException "SsdpInterfaceInputs.MultiInterfaceListener")

                // Clean up managed resources
                removeEventListeners()
                links |> Seq.iter (fun d -> d.Dispose())

            // Clean up native resources
            ()

        do
            inputBuffer.Post NetworkChanged |> ignore
            addEventListeners()

        interface IDisposable with
            override me.Dispose() = dispose true
                                    GC.SuppressFinalize me

        member me.Dispose() = (me :> IDisposable).Dispose()
        override __.Finalize() = dispose false

        member __.Messages = outputBuffer :> ISourceBlock<_>
