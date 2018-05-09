// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

module internal PerformanceTiming =
    open System
    open System.Diagnostics

    [<CompiledName("StopwatchPeriod")>]
    let stopwatchPeriod : float<Us> =
        let p = 1.0 / float Stopwatch.Frequency
        1000000.0<Us> * p // As microseconds

    let private stopwatchToMicroseconds (stopwatch : Stopwatch) : float<Us> =
        float stopwatch.ElapsedTicks * stopwatchPeriod

    let private stopwatchToMilliseconds(stopwatch : Stopwatch) : int64<ms> =
        1L<ms> * stopwatch.ElapsedMilliseconds

    let formatTiming (stopwatch : Stopwatch) =
        let elapsed = stopwatch.Elapsed
        if elapsed >= TimeSpan.FromSeconds(1.0) then
            elapsed.ToString()
        elif elapsed >= TimeSpan.FromSeconds(0.010) then
            sprintf "%d ms" (stopwatchToMilliseconds stopwatch)
        else
            sprintf "%.3f \u00B5s" (stopwatchToMicroseconds stopwatch)

    type TimedFunction<'Result> = delegate of unit -> 'Result

    let timeThis<'Result> (timedFunction : Stopwatch -> 'Result) : struct ('Result * Stopwatch) =

        let sw = Stopwatch.StartNew()
        let result = timedFunction sw
        sw.Stop()
        struct (result, sw)
