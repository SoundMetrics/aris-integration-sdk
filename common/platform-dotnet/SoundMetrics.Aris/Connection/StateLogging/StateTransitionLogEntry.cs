namespace SoundMetrics.Aris.Connection.StateLogging;

internal record StateTransitionLogEntry(
    in LogGrouping Token,
    string CallerContext,
    StateMachineEvent Event,
    StateMachineEventType? EventTypeOverride,
    ConnectionState OldState,
    ConnectionState? AppliedState)
{
    public override string ToString()
        => $"{Token}; [{OldState}]->[{AppliedState}]; [{EventTypeOverride ?? Event.EventType}]; \"{CallerContext}\"";
}
