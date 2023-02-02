// Copyright (c) 2022 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    public interface IAdjustWindowTerminus
    {
        AcousticSettingsRaw MoveWindowStart(
            Distance requestedStart,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency);

        AcousticSettingsRaw MoveWindowEnd(
            Distance requestedEnd,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency);

        AcousticSettingsRaw SelectSpecificRange(
            WindowBounds windowBounds,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency);

        AcousticSettingsRaw SlideWindow(
            Distance requestedStart,
            AcousticSettingsRaw settings,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency);
    }
}
