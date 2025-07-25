using Serilog;
using System.Net;

namespace SoundMetrics.Aris.Connection.StateHandlers;

internal sealed class StateHandlerWatchingForDevice : IStateHandler
{
    public StateEventStatus OnEnter(StateMachineContext context)
    {
        context.ClearConnection();
        Log.Information("Watching for device");
        return StateEventStatus.Okay;
    }

    public StateProcessingResult DoProcessing(
        ConnectionState currentState,
        StateMachineContext context,
        in StateMachineEvent ev)
    {
        switch (ev.EventType, context.DeviceAddress)
        {
            case (StateMachineEventType.Tick, IPAddress deviceAddress):
                Log.Debug("Watching for device address {deviceAddress}", deviceAddress);

                context.DeviceAddress = deviceAddress;
                return new(StateEventStatus.Okay, ConnectionState.AttemptingConnection);

            default:
                return IStateHandler.NoStateChange;
        }
    }

    public StateEventStatus OnLeave(StateMachineContext context)
    {
        // Do nothing.
        return StateEventStatus.Okay;
    }
}
