using Serilog;
using SoundMetrics.Aris.Data;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SoundMetrics.Aris.Network;

public enum FrameStreamReliabilityPolicy
{
    DropPartialFrames,
}

/// Listens for frames. If the FrameStreamReliabilityPolicy calls for dropping partial frames that
/// dropping is done by this type.
internal sealed class FrameStreamListenerOG : IDisposable
{
    public FrameStreamListenerOG(
        IPAddress sinkAddress,
        Action<Frame> reportOnNewFrame,
        FrameStreamReliabilityPolicy frameStreamReliabilityPolicy)
    {
        if (sinkAddress is null)
        {
            throw new ArgumentNullException(nameof(sinkAddress));
        }

        this.reportOnNewFrame = reportOnNewFrame ?? throw new ArgumentNullException(nameof(reportOnNewFrame));
        this.frameStreamReliabilityPolicy = frameStreamReliabilityPolicy;

        udpClient = MakeUdpClient(sinkAddress);
        frameAssembler = new(SendAck, OnNewFrame);

        _ = Task.Run(() => Listen(cts.Token), cts.Token);
    }

    private static UdpClient MakeUdpClient(IPAddress sinkAddress)
    {
        const int ReadBufferSize = 32 * 1024 * 1024;

        var udpClient = new UdpClient(new IPEndPoint(sinkAddress, 0)); // any arbitrary port
        udpClient.Client.ReceiveBufferSize = ReadBufferSize;
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        var localEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint!;
        int localEndPointPort = localEndPoint.Port;

        Debug.WriteLine($"FrameStreamListener: local listen port is {localEndPointPort}");
        return udpClient;
    }

    private void SendAck(int _frameIndex, int _dataOffset)
    {
        // We aren't sending ACKs to the ARIS as it doesn't listen for them.
        // Experimentation showed that having ArisApp listen for ACKs was
        // non-performant.
        //
        //let ack = FrameStream.FramePartAck(FrameIndex = frameIndex, DataOffset = dataOffset)
        //let buf = ack.ToByteArray()
        //udpSender.Send(buf, buf.Length, !remoteEndPoint) |> ignore
    }

    private void OnNewFrame(FrameAccumulatorOG accum)
    {
        if (!disposing)
        {
            var keepFrame =
                frameStreamReliabilityPolicy switch
                {
                    FrameStreamReliabilityPolicy.DropPartialFrames => accum.IsComplete,
                    _ => throw new ArgumentException(nameof(frameStreamReliabilityPolicy))
                };

            if (keepFrame)
            {
                int frameIndex = nextFrameIndex;

                accum.SetFrameIndex(frameIndex);
                if (accum.MakeCompletedFrame(out var frame))
                {
                    reportOnNewFrame.Invoke(frame);
                }

                nextFrameIndex = frameIndex + 1;
            }
        }
    }

    private IPEndPoint UpdateSinkAddress(IPAddress sinkAddress)
    {

        if (!sinkAddress.Equals(((IPEndPoint)udpClient.Client.LocalEndPoint!).Address))
        {
            var oldClient = udpClient;
            var newClient = MakeUdpClient(sinkAddress);
            udpClient = newClient;
            oldClient.Close();
        }

        return (IPEndPoint)udpClient.Client.LocalEndPoint!;
    }

    private void Listen(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Avoid try/catch in the inner loop, it turns out to be a
                // performance hit. So put it in an outer loop.
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        // Not using ReceiveAsync here definitely helps performance.
                        byte[] udpReceiveResult = udpClient.Receive(ref remoteEndPoint);
                        var timestamp = DateTimeOffset.Now;
                        validPacketReceived.OnNext(timestamp);
                        frameAssembler.ProcessPacket(udpReceiveResult, timestamp);
                    }
                }
                catch (Exception ex)
                {
                    // Using UdpClient.Receive() causes an exception on shutdown, but it's
                    // prefered for performance. Log that it's okay and try again.
                    Log.Information(
                        "Ignoring expected exception of type [{exceptionType}] on shutdown.",
                        ex.GetType().Name);
                }
            }
        }
        finally
        {
            completeSignal.Set();
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            disposing = true;

            frameAssembler.Dispose();

            validPacketReceived.OnCompleted();
            validPacketReceived.Dispose();

            cts.Cancel();
            udpClient.Close(); // Close before wait so it will complete.
            completeSignal.Wait();
            completeSignal.Dispose();

            cts.Dispose();
        }
    }

    /// Flushes the current frame and resets to expect any frame index next.
    public void Flush() => frameAssembler.Flush();

    public void SetSinkAddress(IPAddress sinkAddress) => UpdateSinkAddress(sinkAddress);

    public IPEndPoint LocalEndPoint => (IPEndPoint)udpClient.Client.LocalEndPoint!;

    internal IObservable<DateTimeOffset> ValidPacketReceived => validPacketReceived;

    public ProtocolMetricsOG Metrics => frameAssembler.Metrics;

    private readonly FrameAssemblerOG frameAssembler;
    private readonly FrameStreamReliabilityPolicy frameStreamReliabilityPolicy;

    private readonly CancellationTokenSource cts = new();
    private readonly Subject<DateTimeOffset> validPacketReceived = new();
    private readonly ManualResetEventSlim completeSignal = new();
    private readonly Action<Frame> reportOnNewFrame;

    private IPEndPoint remoteEndPoint = new(0L, 0);
    private bool disposed = false;
    private bool disposing = false; // for race conditions
    private int nextFrameIndex = 0;

    // UdpClient is itself a reference type, so it can be updated atomically.
    private UdpClient udpClient;
}
