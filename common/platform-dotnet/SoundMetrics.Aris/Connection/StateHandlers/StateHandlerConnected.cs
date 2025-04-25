using Serilog;
using SoundMetrics.Aris.Connection.Commands;
using System;
using System.Globalization;

namespace SoundMetrics.Aris.Connection.StateHandlers;

internal sealed class StateHandlerConnected : IStateHandler
{
    public void OnEnter(StateMachineContext context)
    {
        Log.Information("Connected to device at {deviceAddress} from {localEndpoint}",
            context.DeviceAddress, context.CommandConnection?.LocalEndpoint);

        context.LatestFramePartTimestamp = DateTimeOffset.Now;

        ApplySettingsRequest(context, context.LatestSettingsRequest);
    }

    public ConnectionState? DoProcessing(StateMachineContext context, in StateMachineEvent ev)
    {
        switch (ev.EventType, ev.CompoundEvent)
        {
            case (StateMachineEventType.Compound, ApplySettingsRequest request):
                ApplySettingsRequest(context, request);
                break;

            case (StateMachineEventType.Compound, DeviceAddressChanged _):
                return ConnectionState.ConnectionTerminated;

            case (StateMachineEventType.MarkFrameDataReceived, _):
                context.LatestFramePartTimestamp = ev.Timestamp;
                break;

            case (StateMachineEventType.Tick, _):
                if (ev.Timestamp >
                    context.LatestFramePartTimestamp + FramePartReceiptTimeout)
                {
                    Log.Information(
                        "Terminating, no frame parts received since {LatestFramePartTimestamp}",
                        context.LatestFramePartTimestamp.ToString("o", CultureInfo.InvariantCulture));
                    return ConnectionState.ConnectionTerminated;
                }
                break;
        }

        return IStateHandler.NoStateChange;
    }

    private static void ApplySettingsRequest(
        StateMachineContext context, ApplySettingsRequest? request)
    {
        if (request is ApplySettingsRequest req
            && context.CommandConnection is CommandConnection connection)
        {
            connection.SendCommand(req);
        }
    }

    public void OnLeave(StateMachineContext context)
    {
        // Do nothing.
    }

    private static readonly TimeSpan FramePartReceiptTimeout = TimeSpan.FromSeconds(5);
}
