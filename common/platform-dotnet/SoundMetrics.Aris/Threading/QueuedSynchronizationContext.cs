// Copyright (c) 2015-2020 Sound Metrics. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SoundMetrics.Aris.Threading
{
    using WorkItem = ValueTuple<SendOrPostCallback, object>;

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

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d is null)
            {
                throw new ArgumentNullException(nameof(d));
            }

            if (disposed) return;

            try
            {
                var workItem = (d, state);
                workQueue.Add(workItem);
            }
            catch (ObjectDisposedException)
            {
                // Asynchronous callbacks try to post here, so they may
                // not know yet that it's disposed. It remains a race condition.
            }
        }

        public override void Send(SendOrPostCallback _d, object _state)
        {
            throw new InvalidOperationException("Send is not supporte");
        }

        private void RunOnCurrentThread(CancellationToken ct)
        {
            try
            {
                try
                {
                    foreach (var (d, state) in workQueue.GetConsumingEnumerable(ct))
                    {
                        d.Invoke(state);
                    }
                }
                catch
                {
                }
            }
            finally
            {
                doneSignal.Set();
            }
        }

        private readonly ManualResetEventSlim doneSignal = new ManualResetEventSlim(false);
        private readonly BlockingCollection<WorkItem> workQueue =
            new BlockingCollection<WorkItem>();
        private readonly CancellationTokenSource cts;

        private bool disposed;
    }
}
