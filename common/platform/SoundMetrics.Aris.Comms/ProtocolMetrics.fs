// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

type ProtocolMetrics =
    { uniqueFrameIndexCount:  uint64
      processedFrameCount:    uint64
      /// Fully complete frames processed.
      completeFrameCount:     uint64
      skippedFrameCount:      uint64
      totalExpectedFrameSize: uint64
      totalReceivedFrameSize: uint64
      totalPacketsReceived:   uint64
      totalPacketsAccepted:   uint64
      unparsablePackets:      uint64 }

module private ProtocolMetricsImpl =
    let empty = 
        { uniqueFrameIndexCount  = 0UL
          processedFrameCount    = 0UL
          completeFrameCount     = 0UL
          skippedFrameCount      = 0UL
          totalExpectedFrameSize = 0UL
          totalReceivedFrameSize = 0UL
          totalPacketsReceived   = 0UL
          totalPacketsAccepted   = 0UL
          unparsablePackets      = 0UL }

type ProtocolMetrics with
    static member Empty = ProtocolMetricsImpl.empty
    static member (+) (a, b) =
        { uniqueFrameIndexCount   = a.uniqueFrameIndexCount   + b.uniqueFrameIndexCount;
          processedFrameCount     = a.processedFrameCount     + b.processedFrameCount;
          completeFrameCount      = a.completeFrameCount      + b.completeFrameCount;
          skippedFrameCount       = a.skippedFrameCount       + b.skippedFrameCount;
          totalExpectedFrameSize  = a.totalExpectedFrameSize  + b.totalExpectedFrameSize;
          totalReceivedFrameSize  = a.totalReceivedFrameSize  + b.totalReceivedFrameSize;
          totalPacketsReceived    = a.totalPacketsReceived    + b.totalPacketsReceived;
          totalPacketsAccepted    = a.totalPacketsAccepted    + b.totalPacketsAccepted;
          unparsablePackets       = a.unparsablePackets       + b.unparsablePackets; }
