// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SoundMetrics.Aris.Core.Raw
{
    public sealed partial class AcousticSettingsRaw
    {
        public AcousticSettingsRaw AdjustRange(
            WindowOperation operation,
            GuidedSettingsMode guidedSettingsMode,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (rangeOperationMap.TryGetValue(operation, out var op))
            {
                return op(
                    this,
                    guidedSettingsMode,
                    observedConditions,
                    useMaxFrameRate,
                    useAutoFrequency);
            }
            else
            {
                throw new NotImplementedException($"Operation '{operation}' is not implemented");
            }
        }

        private static readonly ReadOnlyDictionary<WindowOperation, AdjustRangeFn>
            rangeOperationMap = new ReadOnlyDictionary<WindowOperation, AdjustRangeFn>(
                new Dictionary<WindowOperation, AdjustRangeFn>
                {
                    { WindowOperation.SetShortWindow, ChangeWindow.ToShortWindow },
                    { WindowOperation.SetMediumWindow, ChangeWindow.ToMediumWindow },
                    { WindowOperation.SetLongWindow, ChangeWindow.ToLongWindow },

                    { WindowOperation.MoveWindowStartCloser, ChangeWindow.MoveWindowStartCloser},
                    { WindowOperation.MoveWindowStartFarther, ChangeWindow.MoveWindowStartFarther },
                    { WindowOperation.MoveWindowEndCloser, ChangeWindow.MoveWindowEndCloser },
                    { WindowOperation.MoveWindowEndFarther, ChangeWindow.MoveWindowEndFarther },
                    { WindowOperation.SlideWindowCloser, ChangeWindow.SlideWindowCloser },
                    { WindowOperation.SlideWindowFarther, ChangeWindow.SlideWindowFarther },
                });
    }
}
