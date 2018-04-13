// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System.Diagnostics

[<AbstractClass>]
type ConduitPerformanceReportSink () =

    abstract FrameProcessed : Stopwatch -> unit

    static member public NoOp = NoOpConduitPerformanceReportSink() :> ConduitPerformanceReportSink

and internal NoOpConduitPerformanceReportSink () =
    inherit ConduitPerformanceReportSink()

    override __.FrameProcessed (sw : Stopwatch) = ()

module PerformanceReportSink =

    [<Struct>]
    type DataPoint = {
        SampleCount :       int
        MeanDuration :      float<Us>
        MedianDuration :    float<Us>
    }

    let internal ticksToMicroseconds (ticks : int64) =

        PerformanceTiming.stopwatchPeriod * float ticks

    type internal SampleInfo =
        val mutable SkipCount : int
        val mutable SampleCount : int
        val Samples : int64 array
        val Length : int

        new (size : int, ?skip : int) =
            {
                SkipCount = defaultArg skip 0
                SampleCount = 0
                Samples = Array.zeroCreate<int64> size
                Length = size
            }

        member i.AddSample sample =
            if i.SkipCount > 0 then
                i.SkipCount <- i.SkipCount - 1
            elif i.SampleCount < i.Length then
                i.Samples.[i.SampleCount] <- sample
                i.SampleCount <- i.SampleCount + 1

        member i.Report =
            let samples = i.SampleCount
            if samples = 0 then
                { SampleCount = samples; MeanDuration = 0.0<Us>; MedianDuration = 0.0<Us> }
            else
                let data = i.Samples |> Seq.take samples |> Seq.sort |> Seq.toArray
                let average = data
                              |> Seq.map ticksToMicroseconds
                              |> Seq.average
                let median = data.[samples / 2] |> ticksToMicroseconds

                { SampleCount = samples; MeanDuration = average; MedianDuration = median }

open PerformanceReportSink

type internal SamplePerformanceReportSink (size : int, ?skip : int) =
    inherit ConduitPerformanceReportSink()

    let frameProcessed = SampleInfo(size, defaultArg skip 0)

    override s.FrameProcessed (sw : Stopwatch) = frameProcessed.AddSample sw.ElapsedTicks

    member __.FrameProcessedReport = frameProcessed.Report
