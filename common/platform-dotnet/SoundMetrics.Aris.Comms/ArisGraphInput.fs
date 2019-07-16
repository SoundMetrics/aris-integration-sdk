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
    PingMode:       int32
    BeamCount:      int32
    SampleCount:    int32
    PingsPerFrame:  int32
}
with
    static member FromFrame (header: ArisFrameHeader inref) =
        let cfg = SonarConfig.getPingModeConfig (PingMode.From header.PingMode)
        {
            PingMode = int header.PingMode
            BeamCount  = cfg.ChannelCount
            SampleCount = int header.SamplesPerBeam
            PingsPerFrame = cfg.PingsPerFrame
        }

type ArisOrderedFrame = {
    Header: ArisFrameHeaderBindable
    FrameGeometry: ArisFrameGeometry
    SampleData: NativeBuffer
    Histogram: FrameHistogram
}

type ArisFinishedFrame = {
    Header: ArisFrameHeaderBindable
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


