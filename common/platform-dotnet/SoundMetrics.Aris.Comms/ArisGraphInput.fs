namespace SoundMetrics.Aris.Comms.Experimental

open Aris.FileTypes
open SoundMetrics.NativeMemory
open SoundMetrics.Aris.ReorderCS
open SoundMetrics.Aris.AcousticSettings

// avoiding confusion with SoundMetrics.Aris.Comms & Experimental
type RecordingRequest = SoundMetrics.Aris.Comms.RecordingRequest

type ArisGraphCommand =
    /// Start recording.
    | StartRecording of RecordingRequest

    /// End recording.
    | StopRecording of RecordingRequest

    /// Stop and restart recording.
    | StopStartRecording of stop: RecordingRequest * start: RecordingRequest

    /// Takes a snapshot--the most recent frame received.
    | TakeSnapshot

type ArisRawFrame = SoundMetrics.Aris.Comms.RawFrame

type ArisFrameGeometry = {
    PingMode:       uint32
    BeamCount:      uint32
    SampleCount:    uint32
    PingsPerFrame:  uint32
}
with
    static member FromFrame (f: ArisRawFrame) =
        let cfg = SonarConfig.getPingModeConfig (PingMode.From f.Header.PingMode)
        {
            PingMode = f.Header.PingMode
            BeamCount  = uint32 cfg.ChannelCount
            SampleCount = f.Header.SamplesPerBeam
            PingsPerFrame = uint32 cfg.PingsPerFrame
        }

type ArisOrderedFrame = {
    Header: ArisFrameHeader
    FrameGeometry: ArisFrameGeometry
    SampleData: NativeBuffer
    Histogram: FrameHistogram
}

type ArisFinishedFrame = {
    Header: ArisFrameHeader
    FrameGeometry: ArisFrameGeometry
    SampleData: NativeBuffer
    Histogram: FrameHistogram
}

type ArisFrameType =
    | RawFrame of ArisRawFrame
    | OrderedFrame of ArisOrderedFrame
    | FrameWithBgs of ArisFinishedFrame

type ArisGraphInput =
    /// This is a frame received from the sonar.
    | ArisFrame of ArisFrameType

    /// A command for the graph.
    | GraphCommand of ArisGraphCommand


