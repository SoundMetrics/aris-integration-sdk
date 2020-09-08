using Serilog;
using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal static class Connected
        {
            private static void OnEnter(StateMachineContext context)
            {
                Log.Information("Connected to device at {deviceAddress} from {localEndpoint}",
                    context.DeviceAddress, context.CommandConnection?.LocalEndpoint);

                context.LatestFramePartTimestamp = DateTimeOffset.Now;

                ApplySettingsRequest(context, context.LatestSettingsRequest);
            }

            private static ConnectionState? DoProcessing(
                StateMachineContext context, IMachineEvent? ev)
            {
                switch (ev)
                {
                    case ApplySettingsRequest request:
                        ApplySettingsRequest(context, request);
                        break;

                    case DeviceAddressChanged _:
                        return ConnectionState.ConnectionTerminated;

                    case MarkFrameDataReceived mark:
                        // TODO context.LatestFramePartTimestamp = mark.Timestamp;
                        break;

                    case Tick tick:
                        if (tick.Timestamp >
                            context.LatestFramePartTimestamp + FramePartReceiptTimeout)
                        {
                            return ConnectionState.ConnectionTerminated;
                        }
                        break;
                }

                return default;
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

            private static readonly TimeSpan FramePartReceiptTimeout =
                TimeSpan.FromSeconds(5);

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: OnEnter,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
