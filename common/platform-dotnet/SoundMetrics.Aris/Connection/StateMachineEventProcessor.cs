using Serilog;
using SoundMetrics.Aris.Connection.Commands;
using SoundMetrics.Aris.Connection.StateHandlers;
using SoundMetrics.Aris.Connection.StateLogging;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SoundMetrics.Aris.Connection;

using HandlerMap = Dictionary<ConnectionState, IStateHandler>;

internal sealed class StateMachineEventProcessor : IDisposable
{
    public StateMachineEventProcessor(SystemType systemType)
    {
        this.systemType = systemType;
        events = new BufferedMessageQueue<StateMachineEvent>(DispatchEvent);
    }

    public void Dispose()
    {
        if (dispsosed)
        {
            return;
        }

        dispsosed = true;

        ShutDownQueue();
        events.Dispose();
        context.Dispose();
    }

    public void PostEvent(in StateMachineEvent ev) => events.Post(ev);

    public void PostEvent(StateMachineEventType eventType)
    {
        var stateMacineEvent =
            eventType switch
            {
                StateMachineEventType.Compound =>
                    throw new ArgumentException($"Call not valid for event type {eventType}"),

                _ => new StateMachineEvent(eventType, DateTimeOffset.Now),
            };
        events.Post(stateMacineEvent);
    }

    public void PostEvent(ICompoundMachineEvent compoundEvent)
    {
        var stateMachineEvent =
            new StateMachineEvent(
                StateMachineEventType.Compound,
                DateTimeOffset.Now,
                compoundEvent);
        events.Post(stateMachineEvent);
    }

    private void DispatchEvent(StateMachineEvent ev)
    {
        LogGrouping logGrouping = transitionLog.InitiateLogGroup();
        transitionLog.LogEvent(ref logGrouping, ev);

        try
        {
            try
            {
                switch (ev.EventType, ev.CompoundEvent)
                {
                    case (StateMachineEventType.Compound, ApplySettingsRequest request):
                        state = InvokeStateProcessing(ev, state, context, transitionLog, ref logGrouping);
                        context.LatestSettingsRequest = request;
                        break;

                    case (StateMachineEventType.Compound, Stop stop):
                        try
                        {
                            state = InvokeStateProcessing(ev, state, context, transitionLog, ref logGrouping);
                        }
                        finally
                        {
                            stop.MarkComplete();
                        }
                        break;

                    case (StateMachineEventType.Compound, NewReceiverEndPoint evt):
                        context.ReceiverEndPoint = evt.EndPoint;
                        state = InvokeStateProcessing(ev, state, context, transitionLog, ref logGrouping);
                        break;

                    case (StateMachineEventType.Compound, DeviceAddressChanged evt):
                        // Force specific state handling when the device address changes.
                        context.DeviceAddress = evt.DeviceAddress;
                        state = InvokeStateProcessing(
                            ev,
                            ConnectionState.DeviceAddressChanged,
                            context,
                            transitionLog,
                            ref logGrouping);
                        break;

                    case (StateMachineEventType.Compound, ICompoundMachineEvent evt):
                        state = InvokeStateProcessing(ev, state, context, transitionLog, ref logGrouping);
                        break;

                    case (StateMachineEventType.StartStateMachine, _):
                        state = InvokeStateProcessing(ev, state, context, transitionLog, ref logGrouping);
                        break;

                    case (StateMachineEventType.Tick, _):
                    case (StateMachineEventType.NetworkAddressChanged, _):
                    case (StateMachineEventType.NetworkAvailabilityChanged, _):
                    case (StateMachineEventType.MarkFrameDataReceived, _):
                        state = InvokeStateProcessing(ev, state, context, transitionLog, ref logGrouping);
                        break;

                    default:
                        throw new ArgumentException(
                            $"Unexpected event type: {ev.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    "An error occurred while processing an event of type {eventType} "
                    + "during state {state}: {message}\n"
                    + "{stackTrace}",
                    ev.EventName, state, ex.Message, ex.StackTrace);
                throw;
            }
        }
        finally
        {
            (ev.CompoundEvent as IDisposable)?.Dispose();
        }

        transitionLog.LogEventDone(ref logGrouping, state);
    }

    private static ConnectionState Transition(
        ConnectionState startingState,
        ConnectionState newState,
        StateMachineContext context,
        in StateMachineEvent ev,
        StateTransitionLog transitionLog,
        ref LogGrouping logGrouping)
    {
        if (startingState == newState)
        {
            return startingState;
        }

        ConnectionState currentState = startingState;

        while (newState != ConnectionState.End && newState != currentState)
        {
            StateEventStatus leaveResult = stateHandlers[currentState].OnLeave(context);
            transitionLog.LogLeaveState(ref logGrouping, currentState, newState, leaveResult);

            StateEventStatus enterResult = stateHandlers[newState].OnEnter(context);
            transitionLog.LogEnterState(ref logGrouping, currentState, newState, enterResult);

            StateProcessingResult processingResult = stateHandlers[newState].DoProcessing(newState, context, ev);
            transitionLog.LogProcessing(ref logGrouping, newState, processingResult);

            currentState = newState;
            newState = processingResult.StateOverride ?? newState;
        }

#if DEBUG
        Log.Debug($"remaining state is {currentState} ({logGrouping})");
        Log.Debug($"recent log entries:\n{transitionLog.GetLogTrace()}");
#endif

        return currentState;
    }

    private static ConnectionState InvokeStateProcessing(
        StateMachineEvent ev,
        ConnectionState state,
        StateMachineContext context,
        StateTransitionLog transitionLog,
        ref LogGrouping logGrouping)
    {
        try
        {
            var processingResult = stateHandlers[state].DoProcessing(state, context, ev);
            if (processingResult.StateOverride is ConnectionState newState)
            {
                return Transition(
                        state,
                        newState,
                        context,
                        ev,
                        transitionLog,
                        ref logGrouping);
            }
            else
            {
                return state;
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Exception during state transition: [{ex.Message}]");
            Log.Warning("Terminating connection");
            return Transition(
                    state,
                    ConnectionState.ConnectionLost,
                    context,
                    ev,
                    transitionLog,
                    ref logGrouping);
        }
    }

    private void ShutDownQueue()
    {
        using (var doneSignal = new ManualResetEventSlim(false))
        {
            PostEvent(new Stop(doneSignal));
            if (!doneSignal.Wait(TimeSpan.FromSeconds(30)))
            {
                throw new Exception("ShutDown timed out");
            }
        }
    }

    private static HandlerMap MakeHandlerMap()
    {
        return new HandlerMap
                {
                    { ConnectionState.Start, new FastTransitionTo(ConnectionState.WatchingForDevice) },
                    { ConnectionState.WatchingForDevice, new StateHandlerWatchingForDevice() },
                    { ConnectionState.AttemptingConnection, new StateHandlerAttemptingConnection() },
                    { ConnectionState.Connected, new StateHandlerConnected() },
                    { ConnectionState.DeviceAddressChanged, new FastTransitionTo(ConnectionState.WatchingForDevice) },
                    { ConnectionState.End, new StateHandlerEnd() },
                    { ConnectionState.ConnectionLost, new StateHandlerConnectionLost() }
                };
    }

    private static readonly HandlerMap stateHandlers = MakeHandlerMap();

    private readonly SystemType systemType;
    private readonly BufferedMessageQueue<StateMachineEvent> events;
    private readonly StateMachineContext context = new();
    private readonly StateTransitionLog transitionLog = new(maxLogSize: 30);

    private bool dispsosed;

    private ConnectionState state = ConnectionState.Start;
}
