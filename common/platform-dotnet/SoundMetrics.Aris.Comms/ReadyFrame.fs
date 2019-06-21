// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open SoundMetrics.Aris.ReorderCS


type RecordingState = NotRecording = 0 | Recording = 1

type ReadyFrame = {
    Frame :             RawFrame
    Histogram :         FrameHistogram
    RecordingState :    RecordingState
}
