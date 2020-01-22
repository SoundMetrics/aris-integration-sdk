using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    public class FrameAccumulator
    {
        public void ReceivePacket(ArraySegment<byte> packet)
        {
        }

        public IObservable<object> Frames { get { return frameSubject; } }

        private Subject<object> frameSubject = new Subject<object>();
    }
}
