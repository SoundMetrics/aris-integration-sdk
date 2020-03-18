using SoundMetrics.Aris.SimplifiedProtocol;
using System;

namespace SimplifiedProtocolTestWpfCore
{
    internal interface  ITestOperations
    {
        IObservable<Frame> Frames { get; }

        void StartPassiveMode();
        void StartTestPattern();
    }
}
