// Copyright 2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

open System.Net
open System.Net.NetworkInformation

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
    Properties: IPInterfaceProperties
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

        { Index = index; Name = name; Properties = props }

