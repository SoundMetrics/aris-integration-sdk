namespace SoundMetrics.Aris.Connection;

internal sealed class StateHandlerNull : IStateHandler
{
    public void OnEnter(StateMachineContext context)
    {
        // Do nothing.
    }

    public ConnectionState? DoProcessing(StateMachineContext context, in StateMachineEvent ev)
    {
        // Do nothing.
        return IStateHandler.NoStateChange;
    }

    public void OnLeave(StateMachineContext context)
    {
        // Do nothing.
    }
}
