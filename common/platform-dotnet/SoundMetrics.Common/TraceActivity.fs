namespace SoundMetrics.Common

namespace TryEventTracking

open System
open System.Threading

module internal TraceActivity =

    let processId = System.Diagnostics.Process.GetCurrentProcess().Id

    type CorrelationId = {
        mutable Value:      Guid
        mutable RefCount:   int
    }

    let correlationId =
        let id = new ThreadLocal<CorrelationId>()
        id.Value <- { Value = Guid.Empty; RefCount = 0 }
        id

    // Using side effects to avoid allocations. :-/
    let pushCorrelation () =
        let id = correlationId.Value
        if id.RefCount = 0 then
            id.Value <- Guid.NewGuid()

        id.RefCount <- id.RefCount + 1

    let popCorrelation () =
        let id = correlationId.Value
        if id.RefCount = 0 then
            failwith "Unexpected pop"
        else
            id.RefCount <- id.RefCount - 1
            if id.RefCount = 0 then
                id.Value <- Guid.Empty


