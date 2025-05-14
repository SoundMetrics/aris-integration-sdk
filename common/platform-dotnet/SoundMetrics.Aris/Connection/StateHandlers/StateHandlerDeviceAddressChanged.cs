namespace SoundMetrics.Aris.Connection.StateHandlers;

internal sealed class StateHandlerDeviceAddressChanged : IStateHandler
{
    public void OnEnter(StateMachineContext context)
    {
    }

    public ConnectionState? DoProcessing(
        ConnectionState currentState,
        StateMachineContext context,
        in StateMachineEvent ev)
    {
        context.ClearConnection();
        return ConnectionState.WatchingForDevice;
    }

    public void OnLeave(StateMachineContext context)
    {
    }
}
