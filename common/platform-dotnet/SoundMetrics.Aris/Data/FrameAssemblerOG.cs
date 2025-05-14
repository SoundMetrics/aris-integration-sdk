using FrameStream;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SoundMetrics.Aris.Data;

internal delegate void SendFrameAckFn(int frameIndex, int offset);
internal delegate void HandleFrameFinishedFn(FrameAccumulatorOG frameAccumulator);

internal sealed class FrameAssemblerOG : IDisposable
{
    public FrameAssemblerOG(
        SendFrameAckFn sendAck,
        HandleFrameFinishedFn onFrameFinished)
    {
        this.sendAck = sendAck;
        this.onFrameFinished = onFrameFinished;
        (workQueue, packetQueueLink) = MakeWorkQueue(ProcessWorkUnit);
    }

    private interface IWorkUnit { }

    private record Packet(
        byte[] Data,
        DateTimeOffset Timestamp)
        : IWorkUnit;

    private record Drain(TaskCompletionSource? TaskCompletionSource)
        : IWorkUnit;

    private record struct SkipCounts(ulong Gap, ulong NewSkipCount);

    /// Construct the TPL data graph that makes up the packet queue, which
    /// buffers then invokes processPacket. Returns the graph entry point
    /// and the disposable to tear down the graph.
    private static (BufferBlock<IWorkUnit> PacketQueue, IDisposable PacketQueueLink)
        MakeWorkQueue(Action<IWorkUnit> processWorkUnit)
    {
        var pktQueue = new BufferBlock<IWorkUnit>();
        var pktProcessor = new ActionBlock<IWorkUnit>(processWorkUnit);
        var pktQueueLink = pktQueue.LinkTo(pktProcessor);

        // Return the entry into the graph and the disposable.
        return (pktQueue, pktQueueLink);
    }

    private static void OnFrameIndexAdvanced(
        ref int currentFrameIndex,
        int incomingFrameIndex,
        ref int expectedDataOffset,
        Func<SkipCounts> setSkipCount,
        Action<bool> flush)
    {
        Log.Verbose(
            "Incoming frame indexAdvanced incoming={incoming}; current={current}",
            incomingFrameIndex,
            currentFrameIndex);

        if (currentFrameIndex < 0)
        {
            // This is the first frame we're starting
            Log.Verbose("First known frame index={fi}", incomingFrameIndex);
        }
        else
        {
            // sender moved on to the next frame
            Log.Verbose("Sender moved onto next frame fi={fi}", incomingFrameIndex);

            _ = setSkipCount();
            Log.Verbose("sender moved on to the next frame");
            flush(false);
        }

        Trace.TraceInformation($"Reset currentFrameIndex to {currentFrameIndex}");
        Log.Verbose("Reset current frame index to {incomingFrameIndex}", incomingFrameIndex);

        currentFrameIndex = incomingFrameIndex;
        expectedDataOffset = 0;
    }

    private static void OnReceivedPacketFromPreviousFrame(
        int incomingFrameIndex,
        Func<SkipCounts> setSkipCount)
    {
        // duplicate packet from finished frame
        Log.Verbose("Received packet from previous frame: {fi}", incomingFrameIndex);

        _ = setSkipCount();
        Log.Verbose("duplicate packet from finished frame");
    }

    private static void OnRetrogradeFrameIndex(
        int incomingFrameIndex,
        int currentFrameIndex,
        int incomingDataOffset,
        Action<bool> flush)
    {
        // Sonar may have reset; resync on first frame of a packet; we just checked
        // for incomingDataOffset <> 0 a few lines above.
        Debug.Assert(incomingDataOffset == 0);

        // Flush first (side effect), then reset indexes.
        flush(true);

        var newIndex = incomingFrameIndex;
        currentFrameIndex = newIndex;
        Log.Verbose("Reset frame indexes");
    }

    private static bool OnFrameInProgress(
        int incomingFrameIndex,
        int incomingDataOffset,
        ref int expectedDataOffset,
        FrameStream.FramePart framePart,
        FrameAccumulatorOG accum)
    {

        if (incomingDataOffset == expectedDataOffset)
        {
            accum.AppendFrameData(incomingDataOffset, framePart.Data.ToByteArray());
            expectedDataOffset += framePart.Data.Length;
            Log.Verbose("Accepted packet.l fi={fi}; offset={offset}", incomingFrameIndex, incomingDataOffset);
            return true;
        }
        else
        {
            // Missed a part. Could be caused by a missing packet or by ArisApp resetting and
            // starting over.
            Log.Verbose("Missed a packet");
            return false;
        }
    }

    private static bool OnNoFrameInProgress(
        DateTimeOffset timestamp,
        int incomingFrameIndex,
        int incomingDataOffset,
        ref int expectedDataOffset,
        ref FrameAccumulatorOG? currentFrame,
        FrameStream.FramePart framePart)
    {

        if (incomingDataOffset == 0)
        {
            var accum = new FrameAccumulatorOG(
                incomingFrameIndex,
                framePart.Header.ToByteArray(),
                timestamp,
                framePart.Data.ToByteArray(),
                framePart.TotalDataSize);

            currentFrame = accum;
            accum.AppendFrameData(incomingDataOffset, framePart.Data.ToByteArray());
            expectedDataOffset = framePart.Data.Length;
            Log.Verbose("Accepted a packet. fi={fi}; offset={offset}", incomingFrameIndex, incomingDataOffset);
            return true;
        }
        else
        {
            // Ack will go out asking for the first part of the frame to be resent.
            Log.Verbose("Missed a packet. fi={fi}; offset={offset}", incomingFrameIndex, incomingDataOffset);
            return false;
        }
    }

    private void UpdateMetrics(ProtocolMetricsOG update)
    {
        lock (metricsGuard)
        {
            metrics += update;
        }
    }

    private static ProtocolMetricsOG GetMetricsForFinishedFrame(FrameAccumulatorOG frame)
        => ProtocolMetricsOG.Empty with
        {
            UuniqueFrameIndexCount = 1,
            ProcessedFrameCount = 1,
            CompleteFrameCount = frame.IsComplete ? 1u : 0,
            TotalExpectedFrameSize = (ulong)frame.ExpectedSize,
            TotalReceivedFrameSize = (ulong)frame.BytesReceived,
        };

    /// Assigns a new frame index to the frame (mutation!)
    private static Action<FrameAccumulatorOG> MakeAssignNewFrameIndex()
    {
        // Close over state monotonicFrameIndex.
        int monotonicFrameIndex = 0;    // Frame numbers start at zero. Often this is overwritten
                                        // (e.g. during recording) but we start at zero.

        return (FrameAccumulatorOG frame) =>
        {
            int idx = monotonicFrameIndex++;
            frame.SetFrameIndex(idx);
        };
    }

    /// Assigns a new frame index to the frame (mutation!)
    private readonly Action<FrameAccumulatorOG> AssignNewFrameIndex = MakeAssignNewFrameIndex();

    private void Flush(bool clearFrameIndex)
    {
        lock (stateGuard)
        {
            if (currentFrame is FrameAccumulatorOG frame)
            {
                currentFrame = null;
                AssignNewFrameIndex(frame);
                UpdateMetrics(GetMetricsForFinishedFrame(frame));
                lastFinishedFrameIndex = frame.FrameIndex;

                Log.Verbose("Finished frame fi={fi}; complete={complete}", frame.FrameIndex, frame.IsComplete);

                currentFrameIndex = lastFinishedFrameIndex + 1;
                onFrameFinished(frame);
            }
            else
            {
                // Nothing to do.
            }

            if (clearFrameIndex)
            {
                lastFinishedFrameIndex = -1;
            }
        }
    }

    // Process a packet received on the frame stream.
    private void ReceivePacket(byte[] data, DateTimeOffset timestamp)
    {
        Log.Verbose("Received packet of {length} length", data.Length);

        // Maintain partial metrics for updating metrics below.
        bool acceptedPacket = false;
        ulong skippedFrameCount = 0UL;
        bool parsedOkay = false;

        try // finally
        {
            try // with
            {
                FramePart framePart = FramePart.Parser.ParseFrom(data);
                parsedOkay = true;

                lock (stateGuard)
                {
                    int incomingFrameIndex = framePart.FrameIndex;
                    int incomingDataOffset = framePart.DataOffset;

                    SkipCounts SetSkipCount()
                    {
                        ulong gap = (ulong)(incomingFrameIndex - currentFrameIndex - 1);
                        ulong newValue = skippedFrameCount + gap;
                        skippedFrameCount = newValue;
                        return new(gap, newValue);
                    }

                    // The sonar is responsible for deciding  when to move on to the next frame
                    // so regardless of our state we move on with it.
                    if (incomingFrameIndex > currentFrameIndex)
                    {
                        OnFrameIndexAdvanced(
                            ref currentFrameIndex,
                            incomingFrameIndex,
                            ref expectedDataOffset,
                            SetSkipCount,
                            Flush);
                    }

                    if (incomingFrameIndex < currentFrameIndex && incomingDataOffset != 0)
                    {
                        OnReceivedPacketFromPreviousFrame(incomingFrameIndex, SetSkipCount);
                    }
                    else
                    {
                        if (incomingFrameIndex < currentFrameIndex)
                        {
                            OnRetrogradeFrameIndex(
                                incomingFrameIndex,
                                currentFrameIndex,
                                incomingDataOffset,
                                Flush);
                        }

                        if (currentFrame is FrameAccumulatorOG cf)
                        {
                            acceptedPacket =
                                OnFrameInProgress(
                                    incomingFrameIndex,
                                    incomingDataOffset,
                                    ref expectedDataOffset,
                                    framePart,
                                    cf);
                        }
                        else
                        {
                            acceptedPacket =
                                OnNoFrameInProgress(
                                    timestamp,
                                    incomingFrameIndex,
                                    incomingDataOffset,
                                    ref expectedDataOffset,
                                    ref currentFrame,
                                    framePart);
                        }

                        // NOTE: we're always acking each packet for now; this should change when we
                        // develop strategies for retrying packets.
                        sendAck(incomingFrameIndex, expectedDataOffset);

                        if (expectedDataOffset == framePart.TotalDataSize)
                        {
                            Log.Verbose("Frame complete: fi={fi}", incomingFrameIndex);
                            Flush(false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Could not parse or process frame part: msg={msg}", e.Message);
            }
        }
        finally
        {
            UpdateMetrics(
                ProtocolMetricsOG.Empty with
                {
                    SkippedFrameCount = skippedFrameCount,
                    TotalPacketsReceived = 1UL,
                    TotalPacketsAccepted = acceptedPacket ? 1u : 0,
                    UnparsablePackets = parsedOkay ? 0 : 1u
                });
        }
    }

    private void ProcessWorkUnit(IWorkUnit workUnit)
    {
        ArgumentNullException.ThrowIfNull(workUnit);

        if (workUnit is Packet packet)
        {
            ReceivePacket(packet.Data, packet.Timestamp);
        }
        else if (workUnit is Drain drain)
        {
            if (drain.TaskCompletionSource is TaskCompletionSource source)
            {
                source.SetResult();
            }
        }
        else
        {
            throw new ArgumentException($"Unexpected work unit type: {workUnit.GetType().Name}");
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;

            packetQueueLink.Dispose();
            workQueue.Complete();
            workQueue.Completion.Wait();
        }
    }

    /// Flushes the current frame and resets to expect any frame index next.
    public void Flush() => Flush(true);

    public ProtocolMetricsOG Metrics => metrics;

    public void ProcessPacket(byte[] data, DateTimeOffset timestamp)
        => workQueue.Post(new Packet(data, timestamp));

    public Task DrainAsync()
    {
        var tcs = new TaskCompletionSource();
        _ = workQueue.Post(new Drain(tcs));
        return tcs.Task;
    }

    private readonly object stateGuard = new();
    private readonly object metricsGuard = new();
    private readonly BufferBlock<IWorkUnit> workQueue;
    private readonly IDisposable packetQueueLink;
    private readonly SendFrameAckFn sendAck;
    private readonly HandleFrameFinishedFn onFrameFinished;

    private bool disposed;
    private int currentFrameIndex = -1;
    private int lastFinishedFrameIndex = -1;
    private int expectedDataOffset = 0;
    private FrameAccumulatorOG? currentFrame;
    private ProtocolMetricsOG metrics = ProtocolMetricsOG.Empty;
}
