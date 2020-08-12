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

                    case DeviceAddressChanged addressChanged:
                        return ConnectionState.ConnectionTerminated;
                }

                return default;
            }

            private static void ApplySettingsRequest(
                StateMachineContext context, ApplySettingsRequest? request)
            {
                if (request is ApplySettingsRequest req
                    && context.CommandConnection is CommandConnection connection)
                {
                    Log.Debug("Sending settings type [{settingsType}]",
                        req.Settings.GetType().Name);

                    if (req.Settings is ICommand cmd)
                    {
                        connection.SendCommand(cmd);
                    }
                    else
                    {
                        throw new Exception(
                            $"Settings object {req.Settings.GetType().Name} needs to implement {nameof(ICommand)}");
                    }
                }
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: OnEnter,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
