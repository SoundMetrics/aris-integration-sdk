// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open SoundMetrics.NativeMemory

type internal ConduitOptions = {
    AlternateReordering : (TransformFunction * string) option
}
