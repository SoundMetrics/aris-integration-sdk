// Copyright 2014-2020 Sound Metrics Corp. All Rights Reserved.

using System;

namespace SoundMetrics.Aris.Network
{

    internal static class NetworkConstants
    {
        [Obsolete]
        public const int ArisAvailabilityListenerPortV1 = 56123; // DEFAULT_ARIS_TS_DISCOVERY_LISTEN_PORT  = 56123,

        public const int ArisAvailabilityListenerPortV2 = 56124; // version 2 software
        public const int ArisCommandModuleBeaconPort = 56126;
        public const int ArisDefenderBeaconPort = 56127;
        public const int ArisDefenderCommandPort = 56128;
        public const int ArisSonarAvailabilityAckPort = 56142;    // DEFAULT_ARIS_SONAR_DISCOVERY_ACK_LISTEN_PORT = 56142,

        public const int ArisTsUdpNOListenPort = 56444;           // DEFAULT_ARIS_TS_UDP_NO_LISTEN_PORT1         = 56444,
        public const int ArisSonarUdpNOListenPort = 56555;        // DEFAULT_ARIS_SONAR_UDP_NO_LISTEN_PORT1      = 56555,
        public const int ArisSonarTcpNOListenPort = 56888;        // DEFAULT_ARIS_SONAR_TCP_NO_LISTEN_PORT1      = 56888,

        public const int PlatformDataPort = 700;
    }
}
