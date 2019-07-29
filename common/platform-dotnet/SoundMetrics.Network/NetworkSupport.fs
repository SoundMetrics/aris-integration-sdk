// Copyright 2015-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

module internal NetworkSupport =

    open System.Net
    open System.Net.NetworkInformation

    // Based on
    // http://blogs.msdn.com/b/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx
    let getNetworkAddress (addr : IPAddress) (subnetMask : IPAddress) =

        let addrBytes = addr.GetAddressBytes()
        let subnetBytes = subnetMask.GetAddressBytes()

        if addrBytes.Length <> subnetBytes.Length then
            None
        else
            let netBytes =
                Seq.zip addrBytes subnetBytes
                          |> Seq.map (fun (a, s) -> a &&& s)
                          |> Seq.toArray
            Some (IPAddress(netBytes))


    let isInSameSubnet addr1 addr2 subnetMask =

        match getNetworkAddress addr1 subnetMask, getNetworkAddress addr2 subnetMask with
        | Some n1, Some n2 -> n1 = n2
        | _ -> false

    let findLocalIPAddress remoteIPAddress fallbackAddress =

        let isADesirableInterface (nic: NetworkInterface) =

            let isNpcapLoopback (nic: NetworkInterface) =
                // npcap is not identifying as a loopback, and can
                // live in the same subnet as a link-local interface.
                let up = nic.Name.ToUpper()
                up.Contains("NPCAP") && up.Contains("LOOPBACK")

            nic.NetworkInterfaceType = NetworkInterfaceType.Ethernet
                && not (isNpcapLoopback nic)

        let addrs = seq {
            for nic in NetworkInterface.GetAllNetworkInterfaces()
                            |> Seq.filter isADesirableInterface do
                if nic.OperationalStatus = OperationalStatus.Up then
                    let ipProps = nic.GetIPProperties()
                    for uni in ipProps.UnicastAddresses do
                        let mask = uni.IPv4Mask
                        if isInSameSubnet remoteIPAddress uni.Address mask then
                            yield uni.Address
        }

        let fallback = seq { yield fallbackAddress }
        fallback |> Seq.append addrs |> Seq.head
