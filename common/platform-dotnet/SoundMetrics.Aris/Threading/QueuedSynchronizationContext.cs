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
        public QueuedSynchronizationContext(CancellationToken ct)
        {
            new Thread(() => RunOnCurrentThread(ct)).Start();
        }

        public void Dispose()
        {
            if (!workQueue.IsCompleted)
            {
                Complete();
            }

            doneSignal.Wait();
            workQueue.Dispose();
            doneSignal.Dispose();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d is null)
            {
                throw new ArgumentNullException(nameof(d));
            }

            var workItem = (d, state);
            workQueue.Add(workItem);
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

        public void Complete()
        {
            workQueue.CompleteAdding();
        }

        private readonly ManualResetEventSlim doneSignal = new ManualResetEventSlim();
        private readonly BlockingCollection<WorkItem> workQueue =
            new BlockingCollection<WorkItem>();
    }
}
