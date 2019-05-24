// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open SoundMetrics.Network
open System
open System.Net
open System.Reactive.Subjects

[<Struct>]
type UdpRecieved2 = {
    Timestamp       : DateTime
    Buffer          : ArraySegment<byte>
    RemoteAddress   : IPAddress
    IfcName         : string
    IfcAddress      : IPAddress
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

            let ar = socket.BeginReceiveMessageFrom(
                        buffer,
                        0,
                        buffer.Length,
                        SocketFlags.None,
                        remoteEP,
                        AsyncCallback handleResult,
                        ())

            if ar.CompletedSynchronously then
                handleResult(ar)

        and handleResult (ar : IAsyncResult) =

            let timestamp = DateTime.Now

            try
                let cb =
                    socket.EndReceiveMessageFrom(
                        ar,
                        socketFlagsOut,
                        remoteEP,
                        packetInfo)
                let ifcIndex = (!packetInfo).Interface
                let interfaces = NetworkInterface.GetAllNetworkInterfaces();

                // Thanks, https://github.com/dotnet/corefx/issues/24312
                let struct (ifcName, ifcAddress) =
                    interfaces
                    |> Seq.filter (fun ifc ->
                        ifcIndex = ifc.GetIPProperties().GetIPv4Properties().Index)
                    |> Seq.map (fun ifc ->
                        let ipv4 = ifc.GetIPProperties().UnicastAddresses
                                   |> Seq.filter (fun info ->
                                        info.Address.AddressFamily = AddressFamily.InterNetwork)
                                   |> Seq.exactlyOne
                        struct (ifc.Name, ipv4)
                    )
                    |> Seq.exactlyOne

                handlePacket {  Timestamp = timestamp
                                Buffer = ArraySegment(buffer, 0, cb)
                                RemoteAddress = ((!remoteEP) :?> IPEndPoint).Address
                                IfcName = ifcName
                                IfcAddress = ifcAddress.Address }
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
                                            -> string -> IPAddress
                                            -> 'B option)
                                     : IDisposable * ISubject<'B> =

        let subject = new Subject<'B>()

        let handlePacket {  Timestamp = timestamp
                            Buffer = buffer
                            RemoteAddress = remoteAddress
                            IfcName = ifcName
                            IfcAddress = ifcAddress } =

            match mapPktToBeacon buffer timestamp remoteAddress ifcName ifcAddress with
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
