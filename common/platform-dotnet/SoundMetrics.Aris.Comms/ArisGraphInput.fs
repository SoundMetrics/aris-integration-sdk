namespace SoundMetrics.Aris.Comms.Experimental

open System

type ArisGraphCommand =
    /// Start recording.
    | StartRecording of Guid * (unit -> string)

    /// End recording.
    | EndRecording of Guid

    /// Takes a snapshot--the most recent frame received.
    | TakeSnapshot

type FinishedFrame = FinishedFrame

type ArisFrame =
    | RawFrame of int
    | OrderedFrame of int
    | FrameWithBgs of FinishedFrame

type ArisGraphInput =
    /// This is a frame received from the sonar.
    | ArisFrame of ArisFrame

    /// A command for the graph.
    | Command of ArisGraphCommand


