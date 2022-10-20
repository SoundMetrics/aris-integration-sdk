// Copyright (c) 2022 Sound Metrics Corp.

using SoundMetrics.Aris.Core.Raw;
using System;

namespace SoundMetrics.Aris.Core
{
    public enum GuidedSettingsMode
    {
        Invalid = 0,        // Uninitialized
        FixedSampleCount,   // E.g., when recording or in ROV use
        GuidedSampleCount,  // Chooses a sample count for you
        Free,               // Free raw input
    }

    internal static class GuidedSettingsModeExtensions
    {
        internal static IAdjustWindowTerminus GetAdjustWindowOperations(
            this GuidedSettingsMode mode)
        {
            switch (mode)
            {
                case GuidedSettingsMode.FixedSampleCount:
                    return AdjustWindowTerminusFixed.Instance;

                case GuidedSettingsMode.GuidedSampleCount:
                    return AdjustWindowTerminusGuided.Instance;

                case GuidedSettingsMode.Free:
                    return AdjustWindowTerminusFree.Instance;

                case GuidedSettingsMode.Invalid:
                    throw new ArgumentException($"Invalid mode: {mode}");

                default:
                    throw new ArgumentException($"Unhandled mode: {mode}");
            }
        }
    }
}
