// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

module internal NetworkConstants =

    [<System.Obsolete>]
    let ArisAvailabilityListenerPortV1 = 56123 // DEFAULT_ARIS_TS_DISCOVERY_LISTEN_PORT  = 56123,

    [<Literal>]
    let ArisAvailabilityListenerPortV2 = 56124 // version 2 software
    [<Literal>]
    let ArisCommandModuleBeaconPort = 56126
    [<Literal>]
    let ArisDefenderBeaconPort = 56127
    [<Literal>]
    let ArisDefenderCommandPort = 56128
    [<Literal>]
    let ArisSonarAvailabilityAckPort = 56142    // DEFAULT_ARIS_SONAR_DISCOVERY_ACK_LISTEN_PORT = 56142,

    [<Literal>]
    let ArisTsUdpNOListenPort = 56444           // DEFAULT_ARIS_TS_UDP_NO_LISTEN_PORT1         = 56444,
    [<Literal>]
    let ArisSonarUdpNOListenPort = 56555        // DEFAULT_ARIS_SONAR_UDP_NO_LISTEN_PORT1      = 56555,
    [<Literal>]
    let ArisSonarTcpNOListenPort = 56888        // DEFAULT_ARIS_SONAR_TCP_NO_LISTEN_PORT1      = 56888,

    [<Literal>]
    let PlatformDataPort = 700
