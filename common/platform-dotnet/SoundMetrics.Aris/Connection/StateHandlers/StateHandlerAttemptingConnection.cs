using Serilog;
using SoundMetrics.Aris.Network;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace SoundMetrics.Aris.Connection.StateHandlers;

internal sealed class StateHandlerAttemptingConnection : IStateHandler
{
    public StateEventStatus OnEnter(StateMachineContext context)
    {
        StateEventStatus status = StateEventStatus.Okay;

        Log.Information(
            "Attempting connection to {deviceAddress}",
            context.DeviceAddress);

        InitializeState();

        return status;
    }

    private void InitializeState()
    {
        backoffPeriod = TimeSpan.Zero;
        mostRecentAttempt = default;
        failureLogCountdown = 5;
    }

    public StateProcessingResult DoProcessing(
        ConnectionState currentState,
        StateMachineContext context,
        in StateMachineEvent ev)
    {
        return (ev.EventType, ev.CompoundEvent) switch
        {
            (StateMachineEventType.Compound, DeviceAddressChanged change) =>
                NoteNewDeviceAddress(change),

            (StateMachineEventType.Tick, _) =>
                AttemptConnection(ev.Timestamp),

            _ => default
        };

        StateProcessingResult NoteNewDeviceAddress(DeviceAddressChanged change)
        {
            context.DeviceAddress = change.DeviceAddress;
            return 
                new StateProcessingResult(
                    StateEventStatus.Okay,
                    ConnectionState.DeviceAddressChanged);
        }

        StateProcessingResult AttemptConnection(DateTimeOffset timestamp)
        {
            if (context.CommandConnection is null)
            {
                if (ShouldTryNow(timestamp))
                {

                    if (!(context.DeviceAddress is null))
                    {
                        if (context.ReceiverEndPoint is IPEndPoint receiverEndPoint)
                        {
                            try
                            {
                                Log.Debug("Attempting to connect to {receiverEndPoint}", receiverEndPoint);

                                context.CommandConnection =
                                    CommandConnection.Create(
                                        context.DeviceAddress,
                                        context.SystemType,
                                        receiverEndPoint,
                                        context.Salinity);
                                return new(StateEventStatus.Okay, ConnectionState.Connected);
                            }
                            catch (SocketException socketEx)
                            {
                                var errorMessage = socketEx.ErrorCode switch
                                {
                                    SocketConstants.ECONNREFUSED =>
                                        "Connection refused, device is in use or still booting up",
                                    SocketConstants.ETIMEDOUT =>
                                        "Attempt to connect timed out",

                                    _ => $"Socket error {socketEx.ErrorCode}"
                                };

                                Log.Information("Couldn't connect to {ipAddress}: {exMessage}",
                                    context.DeviceAddress, errorMessage);
                            }
//#pragma warning disable CA1031 // Do not catch general exception types
                            catch (Exception ex)
//#pragma warning restore CA1031 // Do not catch general exception types
                            {
                                if (failureLogCountdown > 0)
                                {
                                    Log.Information("Couldn't connect to {ipAddress}: {exMessage}",
                                        context.DeviceAddress, ex.Message);
                                }
                            }

                            if (failureLogCountdown > 0 && --failureLogCountdown == 0)
                            {
                                Log.Information("Will continue trying to connect");
                            }

                            AdvanceBackoff();
                            mostRecentAttempt = timestamp;
                        }
                        else
                        {
                            Log.Error(
                                $"{nameof(context.DeviceAddress)} is set but {nameof(context.ReceiverEndPoint)} is not");
                        }
                    }
                }
            }

            return IStateHandler.NoStateChange;

            bool ShouldTryNow(DateTimeOffset timestamp)
            {
                var hasAlreadyTried = !(mostRecentAttempt is null);
                var tryNow =
                    !hasAlreadyTried
                    || mostRecentAttempt is DateTimeOffset latestAttempt
                        && timestamp >= latestAttempt + backoffPeriod;
                return tryNow;
            }
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

    public StateEventStatus OnLeave(StateMachineContext context)
    {
        return StateEventStatus.Okay;
    }

    private static readonly TimeSpan MaxBackoffTime = TimeSpan.FromSeconds(5.0);

    private TimeSpan backoffPeriod = TimeSpan.Zero;
    private int failureLogCountdown;
    private DateTimeOffset? mostRecentAttempt;
}
