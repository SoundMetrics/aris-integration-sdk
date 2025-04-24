using Serilog;
using System.Net;

namespace SoundMetrics.Aris.Connection;

internal sealed class StateHandlerWatchingForDevice : IStateHandler
{
    public void OnEnter(StateMachineContext context)
    {
        Log.Information("Watching for device");
    }

    public ConnectionState? DoProcessing(StateMachineContext context, in StateMachineEvent ev)
    {
        switch (ev.EventType, ev.DeviceAddress)
        {
            case (StateMachineEventType.Tick, IPAddress deviceAddress):
                Log.Debug("Watching for device address {deviceAddress}", deviceAddress);

                context.DeviceAddress = deviceAddress;
                return ConnectionState.AttemptingConnection;

            default:
                return IStateHandler.NoStateChange;
        }
    }

    public void OnLeave(StateMachineContext context)
    {
        // Do nothing.
    }
}
