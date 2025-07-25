namespace SoundMetrics.Aris.Connection.StateHandlers;

internal sealed class StateHandlerNull : IStateHandler
{
    public StateEventStatus OnEnter(StateMachineContext context)
    {
        // Do nothing.
        return StateEventStatus.Okay;
    }

    public StateProcessingResult DoProcessing(
        ConnectionState currentState,
        StateMachineContext context,
        in StateMachineEvent ev)
    {
        // Do nothing.
        return IStateHandler.NoStateChange;
    }

    public StateEventStatus OnLeave(StateMachineContext context)
    {
        // Do nothing.
        return StateEventStatus.Okay;
    }
}
