namespace SoundMetrics.Common

open System

module internal TraceActivity =

    open System.Threading

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


open TraceActivity
open Serilog.Core
open Serilog.Events

[<AbstractClass;Sealed>]
type TraceActivity =

    static member CreateProcessEnricher(propertyName: string) : ILogEventEnricher =
        { new ILogEventEnricher with
            member __.Enrich(logEvent: LogEvent, propertyFactory: ILogEventPropertyFactory) =
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty(propertyName, processId))
        }

    static member CreateActivityEnricher(propertyName: string) : ILogEventEnricher =
        { new ILogEventEnricher with
            member __.Enrich(logEvent: LogEvent, propertyFactory: ILogEventPropertyFactory) =
                logEvent.AddOrUpdateProperty(
                    propertyFactory.CreateProperty(propertyName, correlationId.Value.Value))
        }

    static member Correlate (f : Action) =

        pushCorrelation()
        try
            f.Invoke()
        finally
            popCorrelation()

    static member Correlate (f : Func<'T>) : 'T =

        pushCorrelation()
        try
            f.Invoke()
        finally
            popCorrelation()

    static member Correlate (f : unit -> 'T) : 'T =

        pushCorrelation()
        try
            f()
        finally
            popCorrelation()
