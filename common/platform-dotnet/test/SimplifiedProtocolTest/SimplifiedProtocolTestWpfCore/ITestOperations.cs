using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Threading;

namespace SimplifiedProtocolTestWpfCore
{
    using SettingsCookie = UInt32;

    public class ParsedFeedbackFromSonar
    {
        public string RawFeedback { get; set; } = "";
        public uint ResultCode;
        public string ResultString { get; set; } = "";
        public SettingsCookie SettingsCookie;
    }

    public struct AcquireSettings
    {
        public float StartRange;
        public float EndRange;

        public override string ToString()
        {
            return $"start={StartRange}; end={EndRange}";
        }
    }

    internal interface  ITestOperations
    {
        IObservable<Frame> Frames { get; }

        void StartPassiveMode();
        void StartTestPattern();
        void StartDefaultAcquireMode();

        ParsedFeedbackFromSonar StartAcquire(AcquireSettings settings);

        Frame? WaitOnAFrame(
            SynchronizationContext uiSyncContext,
            Predicate<Frame> predicate,
            CancellationToken ct);
    }
}
