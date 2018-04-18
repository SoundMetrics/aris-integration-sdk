// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Aris.FileTypes
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SoundMetrics.Aris.Config
open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open SoundMetrics.NativeMemory

//exception IndeterminateSettingsException

module Frame =

    let FrameHeaderSize = 1024
    let Epoch = DateTime(1970, 1, 1)

[<Extension>]
type ArisFrameHeaderExtensions =
    [<Extension>]
    static member IsReordered(hdr : ArisFrameHeader ref) = (!hdr).ReorderedSamples <> 0u

    [<Extension>]
    static member WindowStart(hdr : ArisFrameHeader ref) = (!hdr).WindowStart

    [<Extension>]
    static member WindowEnd(hdr : ArisFrameHeader ref) = (!hdr).WindowStart + (!hdr).WindowLength

    [<Extension>]
    static member RequestedFocus(hdr : ArisFrameHeader ref) = (!hdr).Focus

    [<Extension>]
    static member ObservedFocus(hdr : ArisFrameHeader ref) = (!hdr).FocusCurrentPosition

    [<Extension>]
    static member IsTelephoto(hdr : ArisFrameHeader ref) = (!hdr).LargeLens <> 0u

    [<Extension>]
    static member SystemType(hdr : ArisFrameHeader ref) = enum<SystemType> (int (!hdr).TheSystemType)

/// Note that the SampleData must be disposed. This record does not do so as
/// buffers are recomposed in new Frame instances.
type Frame = {
    Header : ArisFrameHeader
    SampleData : NativeBuffer
}
with
    member f.BeamCount = uint32 f.SampleData.Length / f.Header.SamplesPerBeam // only available by calculating it

    static member internal HeaderFrom(buf, (timestamp: DateTimeOffset), frameIndex) =
        let h = GCHandle.Alloc(buf, GCHandleType.Pinned)
        try
            let mutable hdr =
                Marshal.PtrToStructure(h.AddrOfPinnedObject(),
                                       typeof<ArisFrameHeader>)
                    :?> ArisFrameHeader

            //-----------------------------------------------------------------
            // Set the PC timestamp. Needs to be done with DateTime rather than
            // DateTimeOffset so that 3pm DST shows up as 3pm on the topside.

            // A tick is 100ns. We need microseconds here.
            let ticks = (timestamp.DateTime - Frame.Epoch).Ticks
            let us = ticks / 10L
            hdr.sonarTimeStamp <- uint64 us

            hdr.FrameIndex <- match frameIndex with | Some fi -> fi | None -> hdr.FrameIndex

            hdr
        finally
            h.Free()

exception IndeterminateSettingsException

[<Struct>]
type internal AcousticSettingsFromFrame = {
    CurrentSettings : AcousticSettingsVersioned
    AppliedSettings : AcousticSettingsApplied
}

[<Extension>]
type ArisFrameExtensions =

    [<Extension>]
    static member internal GetAcousticSettings (f : Frame) =

        let currentSettings = { FrameRate =         f.Header.FrameRate * 1.0f</s>
                                SampleCount =       f.Header.SamplesPerBeam
                                SampleStartDelay =  int f.Header.SampleStartDelay * 1<Us>
                                CyclePeriod =       int f.Header.CyclePeriod * 1<Us>
                                SamplePeriod =      int f.Header.SamplePeriod * 1<Us>
                                PulseWidth =        int f.Header.PulseWidth * 1<Us>
                                PingMode =          PingMode.From (uint32 f.Header.PingMode)
                                EnableTransmit =    f.Header.TransmitEnable <> 0u
                                Frequency =         enum (int f.Header.FrequencyHiLow)
                                Enable150Volts =    f.Header.Enable150V <> 0u
                                ReceiverGain =      float32 f.Header.ReceiverGain }
        let cookie, appliedSettings =
            if f.Header.AppliedSettings > f.Header.ConstrainedSettings && f.Header.AppliedSettings > f.Header.InvalidSettings then
                f.Header.AppliedSettings, Applied { Cookie = f.Header.AppliedSettings; Settings = currentSettings }
            else if f.Header.ConstrainedSettings > f.Header.AppliedSettings && f.Header.ConstrainedSettings > f.Header.InvalidSettings then
                f.Header.ConstrainedSettings, Constrained { Cookie = f.Header.ConstrainedSettings; Settings = currentSettings }
            else if f.Header.InvalidSettings > f.Header.AppliedSettings && f.Header.InvalidSettings > f.Header.ConstrainedSettings then
                f.Header.InvalidSettings, Invalid (f.Header.InvalidSettings, currentSettings)
            else
                raise IndeterminateSettingsException

        let versionedCurrentSettings =
            { Cookie = cookie
              Settings = { FrameRate =          f.Header.FrameRate * 1.0f</s>
                           SampleCount =        f.Header.SamplesPerBeam
                           SampleStartDelay =   int f.Header.SampleStartDelay * 1<Us>
                           CyclePeriod =        int f.Header.CyclePeriod * 1<Us>
                           SamplePeriod =       int f.Header.SamplePeriod * 1<Us>
                           PulseWidth =         int f.Header.PulseWidth * 1<Us>
                           PingMode =           PingMode.From(f.Header.PingMode)
                           EnableTransmit =     f.Header.TransmitEnable <> 0u
                           Frequency =          enum (int f.Header.FrequencyHiLow)
                           Enable150Volts =     f.Header.Enable150V <> 0u
                           ReceiverGain =       float32 f.Header.ReceiverGain } }

        { CurrentSettings = versionedCurrentSettings; AppliedSettings = appliedSettings }
