// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open SoundMetrics.Aris.AcousticSettings.UnitsOfMeasure
open System.Diagnostics

[<AbstractClass>]
type internal ConduitPerfSink () =

    abstract FrameProcessed : Stopwatch -> unit
    abstract FrameReordered : Stopwatch -> unit
    abstract FrameRecorded :  Stopwatch -> unit

    static member public None = NoOpConduitPerfSink() :> ConduitPerfSink

and internal NoOpConduitPerfSink () =
    inherit ConduitPerfSink()

    override __.FrameProcessed (sw : Stopwatch) = ()
    override __.FrameReordered (sw : Stopwatch) = ()
    override __.FrameRecorded  (sw : Stopwatch) = ()

module internal PerfSink =

    [<Struct>]
    type DataPoint = {
        SampleCount :       int
        MeanDuration :      float<Us>
        MedianDuration :    float<Us>
    }

    let ticksToMicroseconds (ticks : int64) =

        PerformanceTiming.stopwatchPeriod * float ticks

    type SampleInfo =
        val mutable SkipCount : int
        val mutable SampleCount : int
        val Samples : int64 array
        val Capacity : int

        new (capacity : int, ?skip : int) =
            {
                SkipCount = defaultArg skip 0
                SampleCount = 0
                Samples = Array.zeroCreate<int64> capacity
                Capacity = capacity
            }

        member i.AddSample sample =
            if i.SkipCount > 0 then
                i.SkipCount <- i.SkipCount - 1
            elif i.SampleCount < i.Capacity then
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

        member i.IsFull = (i.SampleCount = i.Capacity)

open PerfSink

type internal SampledConduitPerfSink (size : int, ?skip : int) =
    inherit ConduitPerfSink()

    let skip' = defaultArg skip 0
    let frameProcessed = SampleInfo(size, skip')
    let frameReordered = SampleInfo(size, skip')
    let frameRecorded =  SampleInfo(size, skip')

    override s.FrameProcessed (sw : Stopwatch) = frameProcessed.AddSample sw.ElapsedTicks
    override s.FrameReordered (sw : Stopwatch) = frameReordered.AddSample sw.ElapsedTicks
    override s.FrameRecorded  (sw : Stopwatch) = frameRecorded.AddSample  sw.ElapsedTicks

    member __.IsFull = frameProcessed.IsFull
    member __.SamplesCollected = frameProcessed.SampleCount
    member __.FrameProcessedReport = frameProcessed.Report
    member __.FrameReorderedReport = frameReordered.Report
    member __.FrameRecordedReport  = frameRecorded.Report
