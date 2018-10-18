// Copyright 2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

    (*
        Provides a public interface for listening to SSDP messages.
    *)

open SsdpInterfaceInputs
open SsdpMessages
open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

/// Client interface for listening to SSDP messages.
type SsdpClient (name : string, multicastLoopback : bool, debugLogging : bool) =

    let mutable disposed = false
    let listener = new MultiInterfaceListener(multicastLoopback, debugLogging)

    let outputBuffer = BufferBlock()

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpClient"))

            disposed <- true

            // Clean up managed resources
            listener.Dispose()

        // Clean up native resources
        ()

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false

    /// TPL Dataflow source of messages.
    member __.Messages = listener.Messages :> ISourceBlock<_>

    /// Useful for debugging.
    member __.Name = name

    /// Request information from a service. `onMessage` may be called on multiple
    /// threads concurrently.
    [<CompiledName("SearchFSharp")>]
    static member SearchAsync (serviceType : string,
                               userAgent : string,
                               timeout : TimeSpan,
                               multicastLoopback : bool,
                               onMessage : (SsdpMessageProperties * SsdpMessage) -> unit) =

        let configUdp (addr : IPAddress) =
            let udp = new UdpClient()
            udp.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
            udp.Client.Bind(IPEndPoint(addr, 0))
            udp.JoinMulticastGroup(SsdpConstants.SsdpEndPointIPv4.Address, addr)
            udp.MulticastLoopback <- multicastLoopback
            udp

        async {

            // Funnel all responses into a single-threaded queue.
            let queue = BufferBlock<_>()
            let processor =
                ActionBlock<_>(fun struct (packet, timestamp, localEP, remoteEP) ->
                                    let props = SsdpMessageProperties.From(packet,timestamp, localEP, remoteEP)
                                    onMessage (props, SsdpMessage.FromResponse packet))
            use _processorLink = queue.LinkTo(processor)

            let packet =
                MSearch
                    {
                        Host        = SsdpConstants.SsdpEndPointIPv4
                        MAN         = "\"ssdp:discover\""
                        MX          = ""
                        ST          = serviceType
                        UserAgent   = userAgent
                    }
                |> SsdpMessage.ToPacket

            let addrs = SsdpNetworkInterfaces.getSspdAddresses() |> Seq.cache
            let sockets = addrs |> Seq.map configUdp |> Seq.toList
            sockets |> List.iter (fun udp -> udp.Send(packet,
                                                      packet.Length,
                                                      SsdpConstants.SsdpEndPointIPv4)
                                                  |> ignore)

            let! results =
                let callback packet timestamp localEP remoteEP =
                    let props = struct (packet, timestamp, localEP, remoteEP)
                    queue.Post(props) |> ignore
                sockets
                |> Seq.map (UdpListenerWithTimeout.listenAsync timeout callback)
                |> Async.Parallel
                // No guarantee here; with a timeout some could starve, but at least there
                // likely won't be many active network interfaces.

            return results.Length > 0
        }

    /// Request information from a service.
    [<CompiledName("SearchAsync")>]
    static member SearchCSharp (serviceType : string,
                                userAgent : string,
                                timeout : TimeSpan,
                                onMessage : Action<(SsdpMessageProperties * SsdpMessage)>) : Task<bool> =

        let callback = fun msg -> onMessage.Invoke(msg)
        Async.StartAsTask(SsdpClient.SearchAsync(serviceType, userAgent, timeout, true, callback))
