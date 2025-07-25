namespace SoundMetrics.Aris.Connection.StateHandlers;

internal sealed class StateHandlerDeviceAddressChanged : IStateHandler
{
    public StateEventStatus OnEnter(StateMachineContext context)
    {
        return StateEventStatus.Okay;
    }

    public StateProcessingResult DoProcessing(
        ConnectionState currentState,
        StateMachineContext context,
        in StateMachineEvent ev)
    {
        context.ClearConnection();
        return new(StateEventStatus.Okay, ConnectionState.WatchingForDevice);
    }

    public StateEventStatus OnLeave(StateMachineContext context)
    {
        return StateEventStatus.Okay;
    }
}
