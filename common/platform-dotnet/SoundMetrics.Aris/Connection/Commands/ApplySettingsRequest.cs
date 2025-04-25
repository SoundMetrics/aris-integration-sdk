using SoundMetrics.Aris.Core.Raw;

namespace SoundMetrics.Aris.Connection.Commands;

/// <summary>
/// Represents a request for new settings.
/// </summary>
internal record ApplySettingsRequest(uint SettingsCookie, AcousticSettingsRaw Settings)
    : ICompoundMachineEvent, IOutgoingCommand;
