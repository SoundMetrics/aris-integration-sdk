// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    internal interface IAdjustWindowTerminus
    {
        AcousticSettingsRaw MoveWindowStart(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds windowBounds,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency);

        AcousticSettingsRaw MoveWindowEnd(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds windowBounds,
            Distance requestedEnd,
            bool useMaxFrameRate,
            bool useAutoFrequency);

        AcousticSettingsRaw SelectSpecificRange(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds windowBounds,
            bool useMaxFrameRate,
            bool useAutoFrequency);

        AcousticSettingsRaw SlideWindow(
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            WindowBounds originalBounds,
            Distance requestedStart,
            bool useMaxFrameRate,
            bool useAutoFrequency);
    }
}
