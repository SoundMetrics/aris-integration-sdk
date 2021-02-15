// Copyright (c) 2015-2020 Sound Metrics. All Rights Reserved.

using Serilog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace SoundMetrics.Aris.Threading
{
    /// A queued (ordered) synchronization context for use in services
    /// or command-line applications.
    /// Based on the implementation found here:
    /// http://blogs.msdn.com/b/pfxteam/archive/2012/01/20/10259049.aspx
    public sealed class QueuedSynchronizationContext : SynchronizationContext, IDisposable
    {
        public QueuedSynchronizationContext(CancellationTokenSource cts)
        {
            this.cts = cts;
            new Thread(() => RunOnCurrentThread(cts.Token)).Start();
        }

        public static QueuedSynchronizationContext RunOnAThread(CancellationTokenSource cts)
        {
            var context = new QueuedSynchronizationContext(cts);
            new Thread(() => context.RunOnCurrentThread(cts.Token)).Start();
            return context;
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                    cts.Cancel();
                    if (!workQueue.IsCompleted)
                    {
                        workQueue.CompleteAdding();
                    }

                    doneSignal.Wait();
                    workQueue.Dispose();
                    doneSignal.Dispose();
                }

                // no unmanaged resources
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (disposed) return;

            try
            {
                var workItem = new WorkItem { Callback = callback, State = state };
                workQueue.Add(workItem);
            }
            catch (ObjectDisposedException)
            {
                // Asynchronous callbacks try to post here, so they may
                // not know yet that it's disposed. It remains a race condition.
            }
        }

        public override void Send(SendOrPostCallback _callback, object? _state)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new InvalidOperationException("Send is not supported");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }

        private void RunOnCurrentThread(CancellationToken ct)
        {
            try
            {
                try
                {
                    foreach (var workItem in workQueue.GetConsumingEnumerable(ct))
                    {
                        workItem.Callback.Invoke(workItem.State);
                    }
                }
                catch (OperationCanceledException)
                {
                    // This is thrown when GetConsumingEnumerable is canceled.
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Log.Error("QueuedSynchronizationContext observed a {exceptionType} exception {exceptionMessage}: {stackTrace}",
                        ex.GetType().Name, ex.Message, ex.StackTrace);
                }
            }
            finally
            {
                doneSignal.Set();
            }
        }

        private struct WorkItem
        {
            public SendOrPostCallback Callback;
            public object? State;
        }

        private readonly ManualResetEventSlim doneSignal = new ManualResetEventSlim(false);
        private readonly BlockingCollection<WorkItem> workQueue =
            new BlockingCollection<WorkItem>();
        private readonly CancellationTokenSource cts;

        private bool disposed;
    }
}
