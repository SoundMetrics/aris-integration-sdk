using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Threading;

namespace SimplifiedProtocolTestWpfCore
{
    internal interface  ITestOperations
    {
        IObservable<Frame> Frames { get; }

        void StartPassiveMode();
        void StartTestPattern();

        Frame? WaitOnAFrame(
            SynchronizationContext uiSyncContext,
            CancellationToken ct);
    }
}
