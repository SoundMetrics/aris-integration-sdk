namespace SoundMetrics.Aris.Connection.StateHandlers;

internal sealed class StateHandlerConnectionLost : IStateHandler
{
    public void OnEnter(StateMachineContext context)
    {
    }

    public ConnectionState? DoProcessing(ConnectionState currentState, StateMachineContext context, in StateMachineEvent ev)
    {
        context.CommandConnection?.Dispose();
        context.CommandConnection = null;

        return ConnectionState.WatchingForDevice;
    }

    public void OnLeave(StateMachineContext context)
    {
    }
}
