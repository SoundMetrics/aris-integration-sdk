// Copyright 2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open System.Net
open System.Net.NetworkInformation
open System.Net.Sockets

[<AutoOpen>]
module private NetworkInterfaceInfoDetails =

    let NoAddress = lazy ( IPAddress 0L )
    let NoIndexFound = -1

    // Acknowledge that the IP properties may be null
    let toValueOption<'T> (t : 'T) : 'T voption =

        if isNull (box t) then
            ValueNone
        else
            ValueSome t

[<Struct>]
type NetworkInterfaceInfo = {
    /// The interface "index" as retrieved via
    /// ifc.GetIPProperties().GetIPv*Properties().Index
    Index:      int

    Name:       string
    Interface:  NetworkInterface
}
with
    static member NoIndex = NoIndexFound
    static member FromNetworkInterface(ifc : NetworkInterface) =

        let name = ifc.Name
        let props = ifc.GetIPProperties()

        let index =
            match props.GetIPv4Properties() |> toValueOption with
            | ValueSome v4 -> v4.Index
            | ValueNone ->
                match props.GetIPv6Properties() |> toValueOption with
                | ValueSome v6 -> v6.Index
                | ValueNone -> NetworkInterfaceInfo.NoIndex

        { Index = index; Name = name; Interface = ifc }

    static member internal Mask(a: byte array, mask: byte array) =

                if a.Length <> mask.Length then
                    invalidArg "c" "Mismatched arguments"

                let output = Array.zeroCreate<byte> a.Length

                for i = 0 to a.Length - 1 do
                    output.[i] <- byte (a.[i] &&& mask.[i])

                output

    static member internal Mask(a: IPAddress, mask: IPAddress) =

            if a.AddressFamily <> AddressFamily.InterNetwork then
                invalidArg "a" (sprintf "Only IPv4 is supported: %A" a)

            let xored = NetworkInterfaceInfo.Mask(a.GetAddressBytes(), mask.GetAddressBytes())
            IPAddress(xored)

    static member internal IsReachable(addr: IPAddress, subnetAddress: IPAddress, ipv4Mask: IPAddress) =
            let a = NetworkInterfaceInfo.Mask(addr, ipv4Mask)
            let b = NetworkInterfaceInfo.Mask(subnetAddress, ipv4Mask)
            a = b

    static member IsAddressReachable(addr: IPAddress,
                                     ifc: NetworkInterface) =

        if addr.AddressFamily = AddressFamily.InterNetwork then
            let props = ifc.GetIPProperties()

            let matchCount =
                props.UnicastAddresses
                |> Seq.filter (fun ua ->
                    ua.Address.AddressFamily = AddressFamily.InterNetwork
                        &&  NetworkInterfaceInfo.IsReachable(addr, ua.Address, ua.IPv4Mask))
                |> Seq.length
            matchCount > 0
        elif addr.AddressFamily = AddressFamily.InterNetworkV6 then
            true
        else
            false
