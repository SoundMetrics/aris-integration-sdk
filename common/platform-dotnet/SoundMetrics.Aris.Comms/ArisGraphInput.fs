namespace SoundMetrics.Aris.Comms.Experimental

open System
open SoundMetrics.Aris.Comms

type ArisGraphCommand =
    /// Start recording.
    | StartRecording of RecordingRequest

    /// End recording.
    | StopRecording of RecordingRequest

    /// Stop and restart recording.
    | StopStartRecording of stop: RecordingRequest * start: RecordingRequest

    /// Takes a snapshot--the most recent frame received.
    | TakeSnapshot

type FinishedFrame = ReadyFrame

type ArisFrameType =
    | RawFrame of Frame
    | OrderedFrame of int
    | FrameWithBgs of FinishedFrame

type ArisGraphInput =
    /// This is a frame received from the sonar.
    | ArisFrame of ArisFrameType

    /// A command for the graph.
    | GraphCommand of ArisGraphCommand


