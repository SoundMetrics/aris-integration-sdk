using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Threading;

namespace SimplifiedProtocolTestWpfCore
{
    using SettingsCookie = UInt32;

    public struct AcquireSettings
    {
        public float StartRange;
        public float EndRange;
    }

    internal interface  ITestOperations
    {
        IObservable<Frame> Frames { get; }

        void StartPassiveMode();
        void StartTestPattern();
        void StartDefaultAcquireMode();

        SettingsCookie StartAcquire(AcquireSettings settings);

        Frame? WaitOnAFrame(
            SynchronizationContext uiSyncContext,
            Predicate<Frame> predicate,
            CancellationToken ct);
    }
}
