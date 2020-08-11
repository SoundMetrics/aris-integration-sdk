using Serilog;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal sealed class Connected
        {
            internal static void OnEnter(StateMachineContext context)
            {
                ApplySettingsRequest(context, context.LatestSettingsRequest);
            }

            internal static ConnectionState? DoProcessing(
                StateMachineContext context, IMachineEvent? ev)
            {
                if (ev is ApplySettingsRequest request)
                {
                    ApplySettingsRequest(context, request);
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

                    var cmd = req.Settings.GenerateCommand();
                    connection.SendCommand(cmd);
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
