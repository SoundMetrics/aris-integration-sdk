// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

module public NetworkConstants =

    let SonarAvailabilityListenerPortV1 = 56123 // DEFAULT_ARIS_TS_DISCOVERY_LISTEN_PORT  = 56123,
    let SonarAvailabilityListenerPortV2 = 56124 // version 2 software
    let CommandModuleCommandPort = 56125
    let CommandModuleBeaconPort = 56126
    let DefenderBeaconPort = 56127
    let DefenderCommandPort = 56128
    let SonarAvailabilityAckPort = 56142        // DEFAULT_ARIS_SONAR_DISCOVERY_ACK_LISTEN_PORT = 56142,

    let TsUdpNOListenPort = 56444               // DEFAULT_ARIS_TS_UDP_NO_LISTEN_PORT1         = 56444,
    let SonarUdpNOListenPort = 56555            // DEFAULT_ARIS_SONAR_UDP_NO_LISTEN_PORT1      = 56555,
    let SonarTcpNOListenPort = 56888            // DEFAULT_ARIS_SONAR_TCP_NO_LISTEN_PORT1      = 56888,

    let BluefinPort = 700                       // DEFAULT_BLUEFIN_LISTEN_PORT
    let PlatformDataPort = 700                  // DEFAULT_BLUEFIN_LISTEN_PORT
