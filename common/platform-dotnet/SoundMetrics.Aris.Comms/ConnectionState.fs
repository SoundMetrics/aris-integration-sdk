// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System.Net
open System.Net.Sockets

type ConnectionState =
    // Be thoughtful about this, we're exposing the state machine state
    | SonarNotFound
    | Connected of ip: IPAddress * cmdLink: TcpClient // ip for ToString on dead state (cmdLink.Client=null)
    | NotConnected of IPAddress
    | ConnectionRefused of IPAddress
    | Closed
with
    override s.ToString() =
        match s with
        | SonarNotFound -> "sonar not found"
        | Connected (ip, _cmdLink) -> sprintf "connected to %s" (ip.ToString())
        | NotConnected ip -> sprintf "not connected to %s" (ip.ToString())
        | ConnectionRefused ip -> sprintf "connection refused from %s" (ip.ToString())
        | Closed -> "closed"
