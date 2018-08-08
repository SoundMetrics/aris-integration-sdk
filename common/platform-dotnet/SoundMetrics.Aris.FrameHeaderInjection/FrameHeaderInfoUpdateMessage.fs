// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.FrameHeaderInjection

open System.Net.Sockets
open System.Runtime.InteropServices

// warning FS0009: Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn "9"'.
#nowarn "9"

module NavInfoUpdateMessage =
    [<AutoOpen>]
    module private NavInfoUpdateMessageImpl =
        [<Struct>]
        [<StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
        type DidsonHeader =
            val mutable nCommand: uint16
            val mutable nSize: uint16
            val mutable nPktType: uint16
            val mutable nPktNum: uint16

        let UpdateCommand = uint16 0xa502
        let PacketType = uint16 0x40
        let PacketNum = uint16 0

    [<CompiledName("Send")>]
    let send (udpClient: UdpClient, ep: System.Net.IPEndPoint, update: HeaderInfoUpdate) : unit =

        let raw = FrameHeaderInfoUpdateRaw.From update
        let headerSize = Marshal.SizeOf(typedefof<DidsonHeader>)
        let payloadSize = Marshal.SizeOf(typedefof<FrameHeaderInfoUpdateRaw>)
        let header = DidsonHeader(nCommand = UpdateCommand,
                                  nSize = uint16 payloadSize,
                                  nPktNum = PacketNum,
                                  nPktType = PacketType)

        let buf = Array.create (payloadSize + headerSize) (byte 0)

        // header
        let unmanagedPtr = Marshal.AllocHGlobal(headerSize)
        Marshal.StructureToPtr(header, unmanagedPtr, false)
        Marshal.Copy(unmanagedPtr, buf, 0, headerSize)
        Marshal.FreeHGlobal(unmanagedPtr)

        // payload
        let unmanagedPtr = Marshal.AllocHGlobal(payloadSize)
        Marshal.StructureToPtr(raw, unmanagedPtr, false)
        Marshal.Copy(unmanagedPtr, buf, headerSize, payloadSize)
        Marshal.FreeHGlobal(unmanagedPtr)

        udpClient.Send(buf, buf.Length, ep) |> ignore
