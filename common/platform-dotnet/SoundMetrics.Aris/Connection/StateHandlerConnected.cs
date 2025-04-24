using Serilog;
using System;
using System.Globalization;

namespace SoundMetrics.Aris.Connection;

internal sealed class StateHandlerConnected : StateHandler
{
    public override void OnEnter(StateMachineContext context)
    {
        Log.Information("Connected to device at {deviceAddress} from {localEndpoint}",
            context.DeviceAddress, context.CommandConnection?.LocalEndpoint);

        context.LatestFramePartTimestamp = DateTimeOffset.Now;

        ApplySettingsRequest(context, context.LatestSettingsRequest);
    }

    public override ConnectionState? DoProcessing(StateMachineContext context, in MachineEvent ev)
    {
        switch (ev.EventType, ev.CompoundEvent)
        {
            case (MachineEventType.Compound, ApplySettingsRequest request):
                ApplySettingsRequest(context, request);
                break;

            case (MachineEventType.Compound, DeviceAddressChanged _):
                return ConnectionState.ConnectionTerminated;

            case (MachineEventType.MarkFrameDataReceived, _):
                context.LatestFramePartTimestamp = ev.Timestamp;
                break;

            case (MachineEventType.Tick, _):
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

        return NoStateChange;
    }

    private static void ApplySettingsRequest(
        StateMachineContext context, ApplySettingsRequest? request)
    {
        if (request is ApplySettingsRequest req
            && context.CommandConnection is CommandConnection connection)
        {
            Log.Debug("Sending settings type [{settingsType}]", req.SettingsType.Name);
            connection.SendCommand(req);
        }
    }

    private static readonly TimeSpan FramePartReceiptTimeout = TimeSpan.FromSeconds(5);
}
