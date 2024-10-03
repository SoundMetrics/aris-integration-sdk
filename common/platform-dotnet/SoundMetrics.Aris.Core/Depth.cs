// Copyright 2014-2024 Sound Metrics corporation

namespace SoundMetrics.Aris.Core;

using System;
using System.Diagnostics;

using static System.Math;
using static Temperature;

public static class Depth
{
    public static Distance CalculateDepth(
        double pressurePSI,
        float salinityPPT,
        Temperature temperature)
    {
        double cf = GetConversionFactor(salinityPPT, temperature);

        double value = (pressurePSI - 14.6959) * 0.702398 / cf;
        return (Distance)value;
    }


    private static int TemperatureToIndex(
        in ReadOnlySpan<float> cfs, 
        Temperature temperature) 
    {
        int maxIdx = cfs.Length - 1;

        // The temperature is the index into cfs. There are 5-degree ranges for each, so
        // idx = lround(temp / 5).

        temperature = Max(Zero, temperature); // Bottom of the scale is zero.

        int idx = (int)Round((temperature / 5).DegreesCelsius); // wraps to really big index on negative
        return Min(maxIdx, idx);
    }

    private static ReadOnlySpan<float> SelectCFRange(float salinityPPT)
    {

        if (salinityPPT >= 35)
        {
            return tempToSeaDepthCFs.Span;
        }
        else if (salinityPPT >= 15)
        {
            return tempToBrackishDepthCFs.Span;
        }
        else
        {
            Debug.Assert(salinityPPT >= 0);
            return tempToFreshDepthCFs.Span;
        }
    }

    private static float SelectCF(
        in ReadOnlySpan<float> cfs,
        Temperature temperature)
    {

        int idx = TemperatureToIndex(cfs, temperature);
        return cfs[idx];
    }

    private static double GetConversionFactor(
        float salinityPPT, 
        Temperature temperature)
    {
        return SelectCF(SelectCFRange(salinityPPT), temperature);
    }

    // 3 sets of conversion factors for 3 different salinities: fresh, brackish and sea water
    // The implied key is water temperature in Celsius.
    // The value is water density.

    private static readonly ReadOnlyMemory<float> tempToFreshDepthCFs = new[] 
    {
        1.000f, // 0C
        1.000f, // 5C
        1.000f, // 10C
        0.999f, // 15C
        0.998f, // 20C
        0.997f, // 25C
        0.996f, // 30C
    };

    private static readonly ReadOnlyMemory<float> tempToBrackishDepthCFs = new[]
    {
        1.012f, // 0C
        1.012f, // 5C
        1.011f, // 10C
        1.011f, // 15C
        1.010f, // 20C
        1.008f, // 25C
        1.007f, // 30C
      };

    private static readonly ReadOnlyMemory<float> tempToSeaDepthCFs = new[]
    {
        1.028f, // 0C
        1.028f, // 5C
        1.027f, // 10C
        1.026f, // 15C
        1.025f, // 20C
        1.023f, // 25C
        1.022f, // 30C
      };
}
