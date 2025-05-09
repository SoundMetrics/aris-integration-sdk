using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace SoundMetrics.Aris.Connection;

internal static class NetworkSupport
{
    // Based on
    // http://blogs.msdn.com/b/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx
    public static IPAddress? GetNetworkAddress(IPAddress addr, IPAddress subnetMask)
    {
        var addrBytes = addr.GetAddressBytes();
        var subnetBytes = subnetMask.GetAddressBytes();

        if (addrBytes.Length != subnetBytes.Length)
        {
            return null;
        }
        else
        {
            var netBytes =
                addrBytes.Zip(subnetBytes)
                    .Select(pair => (byte)(pair.First & pair.Second))
                    .ToArray();
            return new IPAddress(netBytes);
        }
    }

    public static bool IsInSameSubnet(IPAddress addr1, IPAddress addr2, IPAddress subnetMask)
    {
        var a1 = GetNetworkAddress(addr1, subnetMask);
        var a2 = GetNetworkAddress(addr2, subnetMask);

        if (a1 is IPAddress n1 && a2 is IPAddress n2)
        {
            // Important to use Equals(), not `==`.
            bool inSameSubnet = n1.Equals(n2);
            return inSameSubnet;
        }
        else
        {
            return false;
        }
    }

    public static IPAddress FindLocalIPAddress(IPAddress remoteIPAddress, IPAddress fallbackAddress)
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                                .Where(nic => nic.OperationalStatus == OperationalStatus.Up))
        {
            var ipProps = nic.GetIPProperties();
            foreach (var uni in ipProps.UnicastAddresses)
            {
                var mask = uni.IPv4Mask;
                Log.Debug("NIC {nicName} has mask {mask}; is in same subnet: [{isInSameSubnet}]",
                    nic.Name,
                    mask,
                    IsInSameSubnet(remoteIPAddress, uni.Address, mask));
            }
        }

        var addrs = FilterNicAddresses();
        return addrs.Concat([fallbackAddress]).First();

        static bool IsLoopback(NetworkInterface ni) => ni.NetworkInterfaceType == NetworkInterfaceType.Loopback;

        static bool IsNpcapLoopback(NetworkInterface ni)
        {
            // For some reason the npcap loopback interface identifies as
            // 'Ethernet' rather than 'Loopback.'
            var up = ni.Name.ToUpper();
            return up.Contains("NPCAP") && up.Contains("LOOPBACK");
        }

        static bool IsADesiredInterfaceType(NetworkInterface ni)
            => !(IsLoopback(ni) || IsNpcapLoopback(ni));

        IEnumerable<IPAddress> FilterNicAddresses()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                                    .Where(IsADesiredInterfaceType))
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    var ipProps = nic.GetIPProperties();
                    foreach (var uni in ipProps.UnicastAddresses)
                    {
                        var mask = uni.IPv4Mask;
                        Log.Debug("NIC {nicName} has mask {mask}; addr={uniAddress}", nic.Name, mask, uni.Address);
                        if (IsInSameSubnet(remoteIPAddress, uni.Address, mask))
                        {
                            yield return uni.Address;
                        }
                    }
                }
            }
        }
    }
}
