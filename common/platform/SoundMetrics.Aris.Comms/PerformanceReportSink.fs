// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open PerformanceTiming

type PerformanceReportSink =
    abstract FrameProcessed : float<Us> -> unit
