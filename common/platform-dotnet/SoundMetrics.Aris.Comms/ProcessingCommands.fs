// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open SoundMetrics.Aris.Comms

//-----------------------------------------------------------------------------
// General Processing Commands

type internal ProcessingCommand =
    | StartRecording of RecordingRequest
    | StopRecording of RecordingRequest
    | StopStartRecording of stop: RecordingRequest * start: RecordingRequest
