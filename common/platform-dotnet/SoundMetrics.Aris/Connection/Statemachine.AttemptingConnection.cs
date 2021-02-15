﻿using Serilog;
using SoundMetrics.Aris.Network;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

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

            private void OnEnter(StateMachineContext context)
            {
                Log.Information(
                    "Attempting connection to {deviceAddress}",
                    context.DeviceAddress);
                Debug.Assert(context.CommandConnection is null);

                backoffPeriod = TimeSpan.Zero;
                mostRecentAttempt = default;
                failureLogCountdown = 5;
            }

            private ConnectionState? DoProcessing(StateMachineContext context, in MachineEvent ev)
            {
                return (ev.EventType, ev.CompoundEvent) switch
                {
                    (MachineEventType.Compound, DeviceAddressChanged _) =>
                        ConnectionState.ConnectionTerminated,

                    (MachineEventType.Tick, _) =>
                        AttemptConnection(ev.Timestamp),

                    _ => default
                };

                ConnectionState? AttemptConnection(DateTimeOffset timestamp)
                {
                    if (context.CommandConnection is null)
                    {
                        if (ShouldTryNow(timestamp))
                        {

                            if (!(context.DeviceAddress is null))
                            {
                                if (context.ReceiverPort is int port)
                                {
                                    try
                                    {
                                        context.CommandConnection =
                                            CommandConnection.Create(
                                                context.DeviceAddress,
                                                port,
                                                context.Salinity);
                                        return ConnectionState.Connected;
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
#pragma warning disable CA1031 // Do not catch general exception types
                                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
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
                                        $"{nameof(context.DeviceAddress)} is set but {nameof(context.ReceiverPort)} is not");
                                }
                            }
                        }
                    }

                    return default;

                    bool ShouldTryNow(DateTimeOffset timestamp)
                    {
                        var hasAlreadyTried = !(mostRecentAttempt is null);
                        var tryNow =
                            !hasAlreadyTried
                            || (mostRecentAttempt is DateTimeOffset latestAttempt
                                && timestamp >= latestAttempt + backoffPeriod);
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

            private readonly TimeSpan MaxBackoffTime = TimeSpan.FromSeconds(5.0);
            private TimeSpan backoffPeriod = TimeSpan.Zero;
            private int failureLogCountdown;
            private DateTimeOffset? mostRecentAttempt;
        }
    }
}
