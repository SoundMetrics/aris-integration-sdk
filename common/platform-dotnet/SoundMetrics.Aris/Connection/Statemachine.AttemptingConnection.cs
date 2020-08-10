using Serilog;
using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal class AttemptingConnection
        {
            public AttemptingConnection()
            {
                StateHandler =
                    new StateHandler(
                        onEnter: OnEnter,
                        doProcessing: DoProcessing,
                        onLeave: default);
            }

            public StateHandler StateHandler { get; }

            private void OnEnter(StateMachineData data)
            {
                Log.Information(
                    "Attempting connection to {deviceAddress}",
                    data.DeviceAddress);
                Debug.Assert(data.CommandConnection is null);

                backoffPeriod = TimeSpan.Zero;
                mostRecentAttempt = default;
                failureLogCountdown = 5;
            }

            private (ConnectionState?, StateMachineData data)
                DoProcessing(StateMachineData data, IMachineEvent ev)
            {
                switch (ev)
                {
                    case Cycle cycle:
                        return AttemptConnection(cycle.Timestamp);

                    case Tick tick:
                        return AttemptConnection(tick.Timestamp);

                    default:
                        break; // Nothing
                }

                return (default, data);

                (ConnectionState?, StateMachineData data)
                    AttemptConnection(DateTimeOffset timestamp)
                {
                    if (data is StateMachineData d && data.CommandConnection is null)
                    {
                        var hasAlreadyTried = !(mostRecentAttempt is null);
                        var tryNow =
                            !hasAlreadyTried
                            || (mostRecentAttempt is DateTimeOffset latestAttempt
                                && timestamp >= latestAttempt + backoffPeriod);

                        if (tryNow)
                        {
                            try
                            {
                                d.CommandConnection =
                                    CommandConnection.Create(
                                        data.DeviceAddress,
                                        data.ReceiverPort,
                                        data.Salinity);
                                return (ConnectionState.Connected, d);
                            }
                            catch (Exception ex)
                            {
                                if (failureLogCountdown > 0)
                                {
                                    Log.Warning("Couldn't connect to {ipAddress}: {exMessage}",
                                        data.DeviceAddress, ex.Message);
                                }
                            }

                            if (failureLogCountdown > 0 && --failureLogCountdown == 0)
                            {
                                Log.Information("Will continue trying to connect");
                            }

                            AdvanceBackoff();
                            mostRecentAttempt = timestamp;
                        }
                    }

                    return (default, data);
                }
            }

            private void AdvanceBackoff()
            {
                if (backoffPeriod is TimeSpan bt)
                {
                    if (bt < MaxBackoffTime)
                    {
                        var proposedBackoff = bt.Add(TimeSpan.FromSeconds(1));
                        var limitedBackoff =
                            proposedBackoff > MaxBackoffTime ? MaxBackoffTime : proposedBackoff;
                        backoffPeriod = limitedBackoff;
                    }
                }
                else
                {
                    var newBackoff = TimeSpan.FromSeconds(1);
                    backoffPeriod = newBackoff;
                }
            }

            private readonly TimeSpan MaxBackoffTime = TimeSpan.FromSeconds(5.0);
            private TimeSpan backoffPeriod = TimeSpan.Zero;
            private int failureLogCountdown;
            private DateTimeOffset? mostRecentAttempt;
        }
    }
}
