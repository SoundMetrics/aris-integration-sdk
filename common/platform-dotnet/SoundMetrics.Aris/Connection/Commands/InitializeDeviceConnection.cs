using SoundMetrics.Aris.Core;
using System;
using System.Net;

namespace SoundMetrics.Aris.Connection.Commands;

internal record InitializeDeviceConnection(
        DateTimeOffset Timestamp,
        IPEndPoint ReceiverEndPoint,
        Salinity Salinity,
        ApplySettingsRequest ApplySettingsRequest)
    : ICompoundMachineEvent, IOutgoingCommand;
