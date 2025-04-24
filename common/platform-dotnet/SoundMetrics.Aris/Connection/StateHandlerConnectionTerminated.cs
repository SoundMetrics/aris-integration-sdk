using Serilog;

namespace SoundMetrics.Aris.Connection;

internal sealed class StateHandlerConnectionTerminated : StateHandler
{
    public override void OnEnter(StateMachineContext context)
    {
        Log.Information("Connection terminated");

        context.CommandConnection?.Dispose();
        context.CommandConnection = null;
    }

    public override ConnectionState? DoProcessing(StateMachineContext context, in MachineEvent ev)
    {
        return NoStateChange;
    }
}
