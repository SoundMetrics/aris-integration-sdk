// Copyright 2019-2020 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SoundMetrics.Aris.Network
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct NetworkInterfaceInfo
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        private NetworkInterfaceInfo(int index, string name, NetworkInterface ifc)
        {
            Index = index;
            Name = name;
            Interface = ifc;
        }

#pragma warning disable CA1051 // Do not declare visible instance fields
        /// The interface "index" as retrieved via
        /// ifc.GetIPProperties().GetIPv*Properties().Index
        public readonly int Index;

        public readonly string Name;
        public readonly NetworkInterface Interface;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public static readonly int NoIndex = -1;

        public static NetworkInterfaceInfo FromNetworkInterface(NetworkInterface ifc)
        {
            if (ifc is null)
            {
                throw new ArgumentNullException(nameof(ifc));
            }

            var name = ifc.Name;
            var props = ifc.GetIPProperties();

            int index = NoIndex;

            if (props.GetIPv4Properties() is IPv4InterfaceProperties v4)
            {
                index = v4.Index;
            }
            else if (props.GetIPv6Properties() is IPv6InterfaceProperties v6)
            {
                index = v6.Index;
            }

            return new NetworkInterfaceInfo(index, name, ifc);
        }

        internal static byte[] Mask(byte[] a, byte[] mask)
        {
            if (a.Length != mask.Length)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Mismatched argument lengths", nameof(a));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var output = new byte[a.Length];

            for (int i = 0; i < a.Length - 1; ++i)
            {
                output[i] = (byte)(a[i] & mask[i]);
            }

            return output;
        }

        internal static IPAddress Mask(IPAddress a, IPAddress mask)
        {
            if (a.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ArgumentException(
                    $"Only IPv4 is supported: {a}");
            }

            var xored = Mask(a.GetAddressBytes(), mask.GetAddressBytes());
            var masked = new IPAddress(xored);
            return masked;
        }

        internal static bool IsReachable(
            IPAddress addr,
            IPAddress subnetAddress,
            IPAddress ipv4Mask)
        {
            var maskedAddr = Mask(addr, ipv4Mask);
            var maskedSubnet = Mask(subnetAddress, ipv4Mask);
            var areEqual = maskedAddr.Equals(maskedSubnet);
            return areEqual;
        }

        internal static bool IsAddressReachable(
            IPAddress addr,
            NetworkInterface ifc)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                var props = ifc.GetIPProperties();
                var matchCount =
                    props.UnicastAddresses
                        .Count(ua =>
                            ua.Address.AddressFamily == AddressFamily.InterNetwork
                            && IsReachable(addr, ua.Address, ua.IPv4Mask)
                        );

                return matchCount > 0;
            }
            else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
