using Serilog;

namespace SoundMetrics.Aris.Connection;

internal sealed class StateHandlerConnectionTerminated : IStateHandler
{
    public void OnEnter(StateMachineContext context)
    {
        Log.Information("Connection terminated");

        context.CommandConnection?.Dispose();
        context.CommandConnection = null;
    }

    public ConnectionState? DoProcessing(StateMachineContext context, in StateMachineEvent ev)
    {
        return IStateHandler.NoStateChange;
    }

    public void OnLeave(StateMachineContext context)
    {
        // Do nothing.
    }
}
