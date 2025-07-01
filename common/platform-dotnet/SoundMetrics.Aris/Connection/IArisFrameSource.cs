using SoundMetrics.Aris.Data;
using System;

namespace SoundMetrics.Aris.Connection;

public interface IArisFrameSource
{
    IObservable<Frame> Frames { get; }

    uint SerialNumber { get; }
}
