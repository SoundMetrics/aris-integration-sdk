// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

type ProtocolMetrics = {
    UniqueFrameIndexCount:  uint64
    ProcessedFrameCount:    uint64
    /// Fully complete frames processed.
    CompleteFrameCount:     uint64
    SkippedFrameCount:      uint64
    TotalExpectedFrameSize: uint64
    TotalReceivedFrameSize: uint64
    TotalPacketsReceived:   uint64
    TotalPacketsAccepted:   uint64
    UnparsablePackets:      uint64
}

module private ProtocolMetricsDetails =
    let empty = 
        { UniqueFrameIndexCount  = 0UL
          ProcessedFrameCount    = 0UL
          CompleteFrameCount     = 0UL
          SkippedFrameCount      = 0UL
          TotalExpectedFrameSize = 0UL
          TotalReceivedFrameSize = 0UL
          TotalPacketsReceived   = 0UL
          TotalPacketsAccepted   = 0UL
          UnparsablePackets      = 0UL }

type ProtocolMetrics with
    static member Empty = ProtocolMetricsDetails.empty
    static member (+) (a, b) =
        { UniqueFrameIndexCount   = a.UniqueFrameIndexCount   + b.UniqueFrameIndexCount
          ProcessedFrameCount     = a.ProcessedFrameCount     + b.ProcessedFrameCount
          CompleteFrameCount      = a.CompleteFrameCount      + b.CompleteFrameCount
          SkippedFrameCount       = a.SkippedFrameCount       + b.SkippedFrameCount
          TotalExpectedFrameSize  = a.TotalExpectedFrameSize  + b.TotalExpectedFrameSize
          TotalReceivedFrameSize  = a.TotalReceivedFrameSize  + b.TotalReceivedFrameSize
          TotalPacketsReceived    = a.TotalPacketsReceived    + b.TotalPacketsReceived
          TotalPacketsAccepted    = a.TotalPacketsAccepted    + b.TotalPacketsAccepted
          UnparsablePackets       = a.UnparsablePackets       + b.UnparsablePackets }
