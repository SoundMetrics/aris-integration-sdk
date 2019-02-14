// Copyright 2015-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

module NetworkSupport =

    open System.Net
    open System.Net.NetworkInformation
    open System.Net.Sockets

    // Based on
    // http://blogs.msdn.com/b/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx
    [<CompiledName("GetNetworkAddress")>]
    let getNetworkAddress2 (addr : IPAddress, subnetMask : IPAddress) : IPAddress =

        let addrBytes = addr.GetAddressBytes()
        let subnetBytes = subnetMask.GetAddressBytes()

        if addrBytes.Length <> subnetBytes.Length then
            failwithf "Unexpectedly different address lengths"
                
        let netBytes =
            let arr = Array.zeroCreate<byte> addrBytes.Length
            for idx = 0 to arr.Length - 1 do
                arr.[idx] <- addrBytes.[idx] &&& subnetBytes.[idx]
            arr

        IPAddress netBytes


    let internal isInSameSubnet addr1 addr2 subnetMask =

        getNetworkAddress2(addr1, subnetMask) = getNetworkAddress2(addr2, subnetMask)


    [<CompiledName("FindLocalIPAddress")>]
    let internal findLocalIPAddress (remoteIPAddress, fallbackAddress) : IPAddress =

        let addrs = seq {
            for nic in NetworkInterface.GetAllNetworkInterfaces() do
                if nic.OperationalStatus = OperationalStatus.Up then
                    let ipProps = nic.GetIPProperties()
                    for uni in ipProps.UnicastAddresses do
                        let mask = uni.IPv4Mask
                        if isInSameSubnet remoteIPAddress uni.Address mask then
                            yield uni.Address
        }

        let fallback = seq { yield fallbackAddress }
        fallback |> Seq.append addrs |> Seq.head

    [<Struct>]
    type IPv4Interface = {
        Address : IPAddress
        SubnetMask : IPAddress
    }
    with
        member ifc.GetNetworkAddress () : IPAddress =
            getNetworkAddress2 (ifc.Address, ifc.SubnetMask)

        member ifc.IsTargetInSubnet (target : IPAddress) : bool =
            isInSameSubnet target ifc.Address ifc.SubnetMask

    /// Report the currently available IPv4 interfaces.
    [<CompiledName("FindUpIPv4Interfaces")>]
    let findUpIPv4Interfaces () : IPv4Interface array =

        NetworkInterface.GetAllNetworkInterfaces()
            |> Seq.filter (fun nic -> nic.OperationalStatus = OperationalStatus.Up)
            |> Seq.map (fun nic ->
                nic.GetIPProperties().UnicastAddresses
                    |> Seq.filter (fun addr -> addr.Address.AddressFamily = AddressFamily.InterNetwork)
                    |> Seq.map (fun addr -> { Address = addr.Address; SubnetMask = addr.IPv4Mask })
            )
        |> Seq.collect id
        |> Seq.toArray
