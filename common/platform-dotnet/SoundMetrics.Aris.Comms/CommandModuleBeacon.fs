// Copyright 2014 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open SoundMetrics.Aris.Comms
open System.Net

// Command Module Beacon

type internal CommandModuleBeacon = {
    SrcIpAddr : IPAddress

    // sonar_serial_number[] is not currently supported
    ArisCurrent : float32 option
    ArisPower : float32 option
    ArisVoltage : float32 option
    CpuTemp : float32 option
    Revision : uint32 option
}
with
    static member inline ValueOrNone<'T when 'T : equality> (v : 'T)
        = if (v = Unchecked.defaultof<'T>) then None else Some v

    static member internal From(pkt : Udp.UdpReceived) =

        let cms = Aris.CommandModuleBeacon.Parser.ParseFrom(pkt.udpResult.Buffer)
        {
            SrcIpAddr = pkt.udpResult.RemoteEndPoint.Address
            ArisCurrent =   CommandModuleBeacon.ValueOrNone cms.ArisCurrent
            ArisPower =     CommandModuleBeacon.ValueOrNone cms.ArisPower
            ArisVoltage =   CommandModuleBeacon.ValueOrNone cms.ArisVoltage
            CpuTemp =       CommandModuleBeacon.ValueOrNone cms.CpuTemp
            Revision =      CommandModuleBeacon.ValueOrNone cms.Revision
        }
