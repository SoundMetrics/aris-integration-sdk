using SoundMetrics.Aris.Data;
using System;

namespace SoundMetrics.Aris
{
    public sealed class Connection
    {
        public IObservable<Frame> Frames { get; }
    }
}
