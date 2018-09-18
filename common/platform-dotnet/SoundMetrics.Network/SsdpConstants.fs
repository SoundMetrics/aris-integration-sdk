// Copyright 2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

module internal SsdpConstants =
    open System.Net

    /// SSDP uses a multicast address a specific multicast address and port number.
    let SsdpEndPointIPv4 = IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900)
