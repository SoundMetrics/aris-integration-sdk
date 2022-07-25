// Copyright (c) 2022 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    public enum GuidedSettingsMode
    {
        Invalid = 0,        // Likely uninitialized
        FixedSampleCount,   // E.g., when recording or in ROV use
        GuidedSampleCount,  // Chooses a sample count for you
        Free,               // Free raw input
    }

    internal static class GuidedSettingsModeExtensions
    {
        internal static TResult DispatchOperation<TResult>(
            this GuidedSettingsMode mode,
            Func<TResult> onFixed,
            Func<TResult> onGuided,
            Func<TResult> onFree)
        {
            switch (mode)
            {
                case GuidedSettingsMode.FixedSampleCount:
                    return onFixed();

                case GuidedSettingsMode.GuidedSampleCount:
                    return onGuided();

                case GuidedSettingsMode.Free:
                    return onFree();

                case GuidedSettingsMode.Invalid:
                default:
                    throw new ArgumentOutOfRangeException(mode.ToString());
            }
        }
    }
}
