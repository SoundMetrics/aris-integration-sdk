using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SoundMetrics.Aris.Connection.StateLogging;

internal sealed class StateTransitionLog
{
    public delegate string GetDescriptionFn();

    public StateTransitionLog(int maxLogSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLogSize);

        this.maxLogSize = maxLogSize;
    }

    public LogGrouping InitiateLogGroup() => new(++logGroupNumber, 0);

    public void LogEvent(ref LogGrouping logGrouping, in StateMachineEvent ev)
    {
        logGrouping = AdvanceLogGrouping(logGrouping);

        DateTimeOffset now = DateTimeOffset.Now;
        LogGrouping logGroupingClosure = logGrouping;
        string eventDescription = ev.EventName;
        PostEntry(now, GetEventDescription);

        string GetEventDescription() => $"Event. {logGroupingClosure}: {eventDescription}";
    }

    public void LogEventDone(ref LogGrouping logGrouping, ConnectionState state)
    {
        logGrouping = AdvanceLogGrouping(logGrouping);

        DateTimeOffset now = DateTimeOffset.Now;
        LogGrouping logGroupingClosure = logGrouping;
        PostEntry(now, GetEventDescription);

        string GetEventDescription() => $"Event Done. {logGroupingClosure}: state={state}";
    }

    public void LogLeaveState(
        ref LogGrouping logGrouping,
        ConnectionState currentState,
        ConnectionState newState,
        StateEventStatus status)
    {
        logGrouping = AdvanceLogGrouping(logGrouping);

        DateTimeOffset now = DateTimeOffset.Now;
        LogGrouping logGroupingClosure = logGrouping;
        PostEntry(now, GetLeaveDescription);

        string GetLeaveDescription() =>
            $"Leave state. {logGroupingClosure}: current: {currentState}; new: {newState}; result=[{status}]";
    }

    public void LogEnterState(
        ref LogGrouping logGrouping,
        ConnectionState currentState,
        ConnectionState newState,
        StateEventStatus status)
    {
        logGrouping = AdvanceLogGrouping(logGrouping);

        DateTimeOffset now = DateTimeOffset.Now;
        LogGrouping logGroupingClosure = logGrouping;
        PostEntry(now, GetLeaveDescription);

        string GetLeaveDescription() =>
            $"Enter state. {logGroupingClosure}: current: {currentState}; new: {newState}; result=[{status}]";
    }

    public void LogProcessing(
        ref LogGrouping logGrouping,
        ConnectionState state,
        StateProcessingResult result)
    {
        logGrouping = AdvanceLogGrouping(logGrouping);

        DateTimeOffset now = DateTimeOffset.Now;
        LogGrouping logGroupingClosure = logGrouping;
        PostEntry(now, GetLeaveDescription);

        string GetLeaveDescription() =>
            $"Process state. {logGroupingClosure}: state: {state}; result=[{result}]";
    }

    public string GetLogTrace()
    {
        if (logEntries.Count == 0)
        {
            return "";
        }

        StringBuilder buf = new();

        foreach (var logEntry in logEntries)
        {
            buf.Append(logEntry.Timestamp);
            buf.Append(' ');
            buf.AppendLine(logEntry.GetDescription());
        }

        return buf.ToString();
    }

    private void PostEntry(in DateTimeOffset timestamp, GetDescriptionFn getDescription)
    {
        if (logEntries.Count >= maxLogSize)
        {
            logEntries.RemoveAt(logEntries.Count - 1);
        }

        logEntries.Add(new(timestamp, getDescription));
    }

    private LogGrouping AdvanceLogGrouping(in LogGrouping token) => new(token.LogNumber, token.SubPartNumber + 1);

    private record struct LogEntry(
        DateTimeOffset Timestamp,
        GetDescriptionFn GetDescription);

    private readonly int maxLogSize;
    private readonly List<LogEntry> logEntries = [];

    private int logGroupNumber = 0;
}
