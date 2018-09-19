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
open System.Reactive.Subjects
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow

/// Client interface for listening to SSDP messages.
type SsdpClient () =

    let mutable disposed = false
    let messages = new Subject<_>()
    let listener = new MultiInterfaceListener()

    // Shim from TPL dataflow to Reactive.
    let listenerLink = listener.Messages.LinkTo(ActionBlock<_>(messages.OnNext))

    let dispose isDisposing =
        if isDisposing then
            if disposed then
                raise (ObjectDisposedException("SsdpClient"))

            disposed <- true

            // Clean up managed resources
            listenerLink.Dispose()
            listener.Dispose()
            messages.Dispose()

        // Clean up native resources
        ()

    interface IDisposable with
        override me.Dispose () =   dispose true
                                   GC.SuppressFinalize(me)

    member me.Dispose() = (me :> IDisposable).Dispose()
    override __.Finalize() = dispose false

    /// Reactive observable of SSDP messages.
    member __.Messages = messages :> IObservable<_>

    /// TPL Dataflow source of messages, used internally.
    member internal __.MessageSourceBlock = listener.Messages :> ISourceBlock<_>

    /// Request information from a service. `onMessage` may be called on multiple
    /// threads concurrently.
    [<CompiledName("SearchFSharp")>]
    static member SearchAsync (serviceType : string,
                               userAgent : string,
                               timeout : TimeSpan,
                               onMessage : SsdpMessage -> unit) =

        let configUdp (addr : IPAddress) =
            let udp = new UdpClient()
            udp.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
            udp.Client.Bind(IPEndPoint(addr, 0))
            udp.JoinMulticastGroup(SsdpConstants.SsdpEndPointIPv4.Address, addr)
            udp

        async {

            // Funnel all responses into a single-threaded queue.
            let queue = BufferBlock<byte array>()
            let processor =
                ActionBlock<_>(fun packet -> onMessage (SsdpMessage.FromResponse packet))
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
                let callback packet _timestamp = queue.Post(packet) |> ignore
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
                                onMessage : Action<SsdpMessage>) : Task<bool> =

        let callback = fun msg -> onMessage.Invoke(msg)
        Async.StartAsTask(SsdpClient.SearchAsync(serviceType, userAgent, timeout, callback))
