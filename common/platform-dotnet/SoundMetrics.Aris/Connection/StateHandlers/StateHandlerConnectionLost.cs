namespace SoundMetrics.Aris.Connection.StateHandlers;

internal sealed class StateHandlerConnectionLost : IStateHandler
{
    public StateEventStatus OnEnter(StateMachineContext context)
    {
        context.ClearConnection();
        return StateEventStatus.Okay;
    }

    public StateProcessingResult DoProcessing(ConnectionState currentState, StateMachineContext context, in StateMachineEvent ev)
    {
        return new(StateEventStatus.Okay, ConnectionState.WatchingForDevice);
    }

    public StateEventStatus OnLeave(StateMachineContext context)
    {
        return StateEventStatus.Okay;
    }
}
