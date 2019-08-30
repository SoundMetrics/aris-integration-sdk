// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open System
open System.Net
open System.Reactive.Subjects

[<Struct>]
type UdpRecieved2 = {
    Timestamp:      DateTime
    Buffer:         ArraySegment<byte>
    RemoteAddress:  IPAddress
    InterfaceInfo:  NetworkInterfaceInfo
}

module internal BeaconObserver2Details =
    open System.Net.Sockets
    open System.Threading
    open System.Net.NetworkInformation

    let private mkSocketListener port handlePacket : IDisposable =

        let socket =
            let s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            s.Bind(new IPEndPoint(IPAddress.Any, port));
            s

        let remoteEP : EndPoint ref = ref (IPEndPoint(IPAddress.Any, 0) :> EndPoint)
        let maxBufferNeeded = 1024
        let buffer = Array.zeroCreate<byte> maxBufferNeeded
        let doneSignal = new ManualResetEventSlim()
        let socketFlagsOut = ref SocketFlags.None;
        let packetInfo = ref (IPPacketInformation())

        let signalDone () = doneSignal.Set()

        let rec queueReceive () =

            socket.BeginReceiveMessageFrom(
                buffer,
                0,
                buffer.Length,
                SocketFlags.None,
                remoteEP,
                AsyncCallback handleResult,
                ()) |> ignore


        and handleResult (ar : IAsyncResult) =

            let timestamp = DateTime.Now

            try
                let cb =
                    socket.EndReceiveMessageFrom(
                        ar,
                        socketFlagsOut,
                        remoteEP,
                        packetInfo)
                let receiverIfcIndex = (!packetInfo).Interface
                let interfaces = NetworkInterface.GetAllNetworkInterfaces();

                // Thanks, https://github.com/dotnet/corefx/issues/24312
                let ifcInfo =
                    interfaces
                    |> Seq.filter (fun ifc ->
                        receiverIfcIndex = ifc.GetIPProperties().GetIPv4Properties().Index)
                    |> Seq.map (fun ifc ->
                        let ipv4 = ifc.GetIPProperties().UnicastAddresses
                                   |> Seq.filter (fun info ->
                                        info.Address.AddressFamily = AddressFamily.InterNetwork)
                                   |> Seq.exactlyOne
                        NetworkInterfaceInfo.FromNetworkInterface ifc
                    )

                match ifcInfo |> Seq.tryExactlyOne with
                | Some ifcInfo ->
                    handlePacket {  Timestamp = timestamp
                                    Buffer = ArraySegment(buffer, 0, cb)
                                    RemoteAddress = ((!remoteEP) :?> IPEndPoint).Address
                                    InterfaceInfo = ifcInfo }
                | None -> () // Couldn't find an interface; possibly a stale beacon that's
                             // pulled from the network stack just as the network interface
                             // went away. So be friendly if there's no interface found.
                             // https://github.com/SoundMetrics/aris-integration-sdk/issues/129

                queueReceive()
            with
                :? ObjectDisposedException ->
                    // The socket was closed.
                    signalDone()

        queueReceive()

        {
            new IDisposable with
                member __.Dispose() =
                    socket.Close()
                    doneSignal.Wait(TimeSpan.FromSeconds(2.0)) |> ignore
                    doneSignal.Dispose()
        }


    let private mkBeaconObserver<'B> (port : int)
                                     (mapPktToBeacon :
                                        ArraySegment<byte> -> DateTime -> IPAddress
                                            -> NetworkInterfaceInfo
                                            -> 'B option)
                                     : IDisposable * ISubject<'B> =

        let subject = new Subject<'B>()

        let handlePacket {  Timestamp = timestamp
                            Buffer = buffer
                            RemoteAddress = remoteAddress
                            InterfaceInfo = ifcInfo } =

            match mapPktToBeacon buffer timestamp remoteAddress ifcInfo with
            | Some beacon -> subject.OnNext(beacon)
            | None -> ()

        let listener = mkSocketListener port handlePacket

        let disposable =
            { new IDisposable with
                member __.Dispose() =
                    listener.Dispose()
                    subject.OnCompleted()
                    subject.Dispose()
            }

        disposable, subject :> ISubject<'B>


    let mkExplorerBeaconObserver () : IDisposable * ISubject<ArisBeacon2> =

        mkBeaconObserver<ArisBeacon2>
            NetworkConstants.ArisAvailabilityListenerPortV2
            BeaconListener.toArisExplorerOrVoyagerBeacon2


    let mkCommandModuleBeaconObserver () : IDisposable * ISubject<ArisCommandModuleBeacon2> =

        mkBeaconObserver<ArisCommandModuleBeacon2>
            NetworkConstants.ArisCommandModuleBeaconPort
            BeaconListener.toArisCommandModuleBeacon2

type BeaconObserver2<'B> internal (disposer : IDisposable, subject : ISubject<'B>) =

    let mutable disposed = false

    let dispose () =

        if not disposed then
            disposed <- true
            disposer.Dispose()

    interface IDisposable with
        member __.Dispose() = dispose()

    member __.Beacons = subject :> IObservable<'B>

module BeaconObserver2 =

    [<CompiledName("CreateExplorerBeaconObserver")>]
    let mkExplorerBeaconObserver () =
        new BeaconObserver2<ArisBeacon2>(
            BeaconObserver2Details.mkExplorerBeaconObserver())

    [<CompiledName("CreateCommandModuleBeaconObserver")>]
    let mkCommandModuleBeaconObserver () =
        new BeaconObserver2<ArisCommandModuleBeacon2>(
            BeaconObserver2Details.mkCommandModuleBeaconObserver())
