// Copyright (c) 2022 Sound Metrics Corp.

using System.Diagnostics.CodeAnalysis;

namespace SoundMetrics.Aris.Core.Raw
{
    public enum RangeSelectionAction
    {
        Invalid = 0,
        MoveWindowStart,
        MoveWindowEnd,
        MoveEntireWindow,
        UseSpecificRange,
    }

    /// <summary>
    /// Diagnostic feedback for range operations.
    /// Meant for internal use only.
    /// </summary>
    public sealed class RangeSelectionFeedback
    {
        // Inputs
        public RangeSelectionAction RangeSelectionAction { get; set; }
        public GuidedSettingsMode GuidedSettingsMode { get; set; }
        public WindowBounds StartingWindowBounds { get; set; }
        public Distance? RequestedStart { get; set; }
        public Distance? RequestedEnd { get; set; }
        public WindowBounds? RequestedWindow { get; set; }
        public ObservedConditions ObservedConditions { get; set; }
        public AcousticSettingsRaw SettingsIn { get; set; }

        // Outputs
        public WindowBounds FinalWindowBounds { get; set; }
        public AcousticSettingsRaw SettingsOut { get; set; }
    }
}
