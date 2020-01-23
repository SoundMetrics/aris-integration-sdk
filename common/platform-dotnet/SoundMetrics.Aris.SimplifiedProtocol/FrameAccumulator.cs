using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    public class FrameAccumulator
    {
        public void ReceivePacket(byte[] packet)
        {
            if (FramePartHeaderExtensions.FromBytes(packet, out var packetHeader))
            {
                if (packetHeader.PartNumber == 0)
                {
                    wip = new WorkInProgress
                    {
                        CurrentPartNumber = packetHeader.PartNumber,
                        FrameIndex = packetHeader.FrameIndex,
                    };

                    //AccumulateSamples(packetHeader, wip);
                }
                else if (packetHeader.FrameIndex != wip.FrameIndex)
                {
                    wip = new WorkInProgress();
                }
                else if (packetHeader.PartNumber == wip.CurrentPartNumber + 1)
                {

                }
            }
            else
            {
                // Invalid header
            }
        }

        //private static WorkInProgress AccumulateSamples(
        //    in FramePacketHeader header,
        //    in WorkInProgress wip,

        //    )
        //{

        //}

        public IObservable<object> Frames { get { return frameSubject; } }

        private readonly Subject<object> frameSubject = new Subject<object>();

        private WorkInProgress wip = new WorkInProgress();

        private struct WorkInProgress
        {
            public uint CurrentPartNumber;
            public uint FrameIndex;
            public object Frame;
            public int DataReceived;
        }
    }
}
