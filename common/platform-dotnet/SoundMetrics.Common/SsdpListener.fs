// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

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

module private SsdpNetworkInterfaces =

    //let nics = NetworkInterface.GetAllNetworkInterfaces()
    //for nic in nics |> Seq.filter (fun nic ->
    //    nic.OperationalStatus = NetworkInformation.OperationalStatus.Up
    //    && nic.NetworkInterfaceType <> NetworkInterfaceType.Loopback) do

    //    if nic.Supports(System.Net.NetworkInformation.NetworkInterfaceComponent.IPv4) then
    //        printfn "%s" nic.Description

    //        printfn "  Type     = %A" nic.NetworkInterfaceType
    //        printfn "  OpStatus = %A" nic.OperationalStatus
    //        printfn "  Speed    = %A" nic.Speed
    //        printfn "  PhysAddr = %A" (nic.GetPhysicalAddress())

    //        let props = nic.GetIPProperties()
    //        printfn "  Unicasts ="
    //        props.UnicastAddresses |> Seq.map (fun a ->
    //                                    let a = (a :> IPAddressInformation)
    //                                    a.Address, a.IsTransient)
    //                               |> Seq.iter (fun (addr, transient) ->
    //                                printfn "    %A (transient=%A)" addr transient)

    //        let v4Props = props.GetIPv4Properties()

    //        if isNull v4Props then
    //            printfn "   No IPv4 information available"
    //        else
    //            printfn "    Index          = %d" v4Props.Index
    //            printfn "    MTU            = %d" v4Props.Mtu
    //            printfn "    APIPA active   = %A" v4Props.IsAutomaticPrivateAddressingActive
    //            printfn "    APIPA enabled  = %A" v4Props.IsAutomaticPrivateAddressingEnabled

    let getNics () =

        NetworkInterface.GetAllNetworkInterfaces()
            |> Seq.filter (fun nic ->
                    nic.OperationalStatus = OperationalStatus.Up
                    && nic.Supports(NetworkInterfaceComponent.IPv4)
                    && nic.NetworkInterfaceType <> NetworkInterfaceType.Loopback)
            |> Seq.toArray

module private SsdpInterfaceInputs =
    open System.Threading.Tasks.Dataflow
    open System.Net.Sockets
    open System.Net.NetworkInformation

    // SSDP uses a multicast address a specific multicast address and port number.
    let SsdpAddressIPv4 = IPAddress.Parse("239.255.255.250")
    let SsdpPortIPv4 = 1900
    let SsdpEndPointIPv4 = IPEndPoint(SsdpAddressIPv4, SsdpPortIPv4)

    type MsgReceived = { UdpResult : UdpReceiveResult; Timestamp : DateTime }

    let allowedAddressFamilies =
        [ AddressFamily.InterNetwork; AddressFamily.InterNetworkV6 ]
        |> Set

    let selectUnicastAddress (nic : NetworkInterface) =

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

    // It would be nice if NetworkAddressChanged returned some useful information, but alas...
    // so we just have a catch-all input that triggers some contemplation of what interfaces
    // are still available. Then, to serialize operations, we push packets in through the same
    // buffer block.
    type SsdpInterfaceInputs = NetworkChanged | Packet of MsgReceived

    type InterfaceListener (addr : IPAddress, target : ITargetBlock<SsdpInterfaceInputs>) =

        let mutable disposed = false
        let cts = new CancellationTokenSource ()
        let doneSignal = new ManualResetEventSlim ()
        let udp = new UdpClient()

        let rec listen () =

            // Switched to task-based rather than wrapping in Async as sometimes the Async
            // just never completes. And that prevents the process from terminating.

            let task = udp.ReceiveAsync()
            let action = Action<System.Threading.Tasks.Task<UdpReceiveResult>>(fun t ->
                let now = DateTime.Now
                let keepGoing = t.IsCompleted && not t.IsFaulted && not cts.IsCancellationRequested
                if keepGoing then
                    target.Post (Packet { UdpResult = task.Result; Timestamp = now }) |> ignore
                    listen()
                else
                    doneSignal.Set() )
            task.ContinueWith(action) |> ignore

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
            udp.Client.Bind (new IPEndPoint(IPAddress.Any (*IPAddress.Parse("192.168.10.145")*), SsdpPortIPv4)) // TODO .Any on right ifc
            udp.JoinMulticastGroup(SsdpAddressIPv4, IPAddress.Any (*IPAddress.Parse("192.168.10.145")*))
            listen()

        interface IDisposable with
            override me.Dispose() = dispose true
                                    GC.SuppressFinalize me
        member me.Dispose() = (me :> IDisposable).Dispose()
        override __.Finalize() = dispose false


    type MultiInterfaceListener () =

        let mutable disposed = false
        let mutable interfaceMap = Map.empty<string, Interface>
        let mutable listenerMap = Map.empty<string, InterfaceListener>
        let inputBuffer = BufferBlock<_>()
        let outputBuffer = BufferBlock<_>()

        let processInterfaceInput = function
            | NetworkChanged ->
                let newNics =
                    SsdpNetworkInterfaces.getNics()
                        |> Seq.map Interface.FromNetworkInterface
                        |> Seq.choose id // drop Nones
                        |> Seq.map (fun ifc -> ifc.Id, ifc)
                        |> Map.ofSeq

                let expiredListeners =
                    interfaceMap |> Seq.filter (fun kvp -> not (newNics.ContainsKey kvp.Key))
                for kvp in expiredListeners do
                    interfaceMap <- interfaceMap.Remove(kvp.Key)
                    let old = listenerMap.[kvp.Key]
                    old.Dispose()
                    listenerMap <- listenerMap.Remove(kvp.Key)

                let newListeners =
                    newNics |> Seq.filter (fun kvp -> not (interfaceMap.ContainsKey kvp.Key))
                for kvp in newListeners do
                    interfaceMap <- interfaceMap.Add(kvp.Key, kvp.Value)
                    let listener = new InterfaceListener(kvp.Value.Address, inputBuffer)
                    listenerMap <- listenerMap.Add(kvp.Key, listener)
            | Packet udpResult -> ()



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

        member __.Packets = outputBuffer :> ISourceBlock<_>


module internal SsdpListener =
    open System.Net.Sockets
    open System.Reactive.Subjects

    // SSDP uses a multicast address a specific multicast address and port number.
    let SsdpAddressIPv4 = IPAddress.Parse("239.255.255.250")
    let SsdpPortIPv4 = 1900
    let SsdpEndPointIPv4 = IPEndPoint(SsdpAddressIPv4, SsdpPortIPv4)

    type MsgReceived = { UdpResult : UdpReceiveResult; Timestamp : DateTime }

    type SsdpReaderWriter () =

        // Shutdown
        let mutable disposed = false
        let cts = new CancellationTokenSource ()
        let doneSignal = new ManualResetEventSlim ()

        let _remove = new SsdpInterfaceInputs.MultiInterfaceListener()

        // Get & publish packets
        let packetSubject = new Subject<MsgReceived> ()
        let udp = new UdpClient()

        let rec listen () =

            // Switched to task-based rather than wrapping in Async as sometimes the Async
            // just never completes. And that prevents the process from terminating.

            let task = udp.ReceiveAsync()
            let action = Action<System.Threading.Tasks.Task<UdpReceiveResult>>(fun t ->
                let now = DateTime.Now
                let keepGoing = t.IsCompleted && not t.IsFaulted && not cts.IsCancellationRequested
                if keepGoing then
                    packetSubject.OnNext { UdpResult = task.Result; Timestamp = now }
                    listen()
                else
                    doneSignal.Set() )
            task.ContinueWith(action) |> ignore

        let dispose isDisposing =
            if isDisposing then
                // Clean up managed resources
                if disposed then
                    raise (ObjectDisposedException "ReaderWriter")

                disposed <- true

                // Stop listening to the socket
                cts.Cancel ()
                udp.Close () // Because UdpClient doesn't know about cancellation--may make it throw.
                doneSignal.Wait ()

                // Clean up
                packetSubject.OnCompleted ()

                let otherDisposables : IDisposable list = [udp; packetSubject; cts; doneSignal]
                otherDisposables |> List.iter (fun d -> if d <> null then d.Dispose())

            // Clean up native resources
            ()


        do
            udp.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
            udp.Client.Bind (new IPEndPoint(IPAddress.Any (*IPAddress.Parse("192.168.10.145")*), SsdpPortIPv4)) // TODO .Any on right ifc
            udp.JoinMulticastGroup(SsdpAddressIPv4, IPAddress.Any (*IPAddress.Parse("192.168.10.145")*))
            listen()

        interface IDisposable with

            override me.Dispose() = dispose true
                                    GC.SuppressFinalize me
        member me.Dispose() = (me :> IDisposable).Dispose()
        override __.Finalize() = dispose false

        member __.Packets : IObservable<MsgReceived> = packetSubject :> IObservable<_>

        member __.SendUnicastAsync (buf : byte array) (ep : IPEndPoint) = udp.SendAsync(buf, buf.Length, ep)

        member __.SendMulticast (buf : byte array) = udp.SendAsync(buf, buf.Length, SsdpEndPointIPv4)
