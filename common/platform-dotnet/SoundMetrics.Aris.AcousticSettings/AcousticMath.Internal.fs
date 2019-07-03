// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open System


type SoundSpeed = float<m/s>
type FrameRate = float</s>

type Salinity =
    | Fresh = 0
    | Brackish = 15
    | Seawater = 35

module internal AcousticMathDetails =

        // Calculate the speed of sound based on temperature, depth and salinity, per
        // the associated documents.
        //
        // We're calculating to the first-order depth correction, nothing beyond that.
        //
        // C = 1402.5 + (5 * T) - (5.44e-2 * T*T) + (2.1e-4 * T*T*T) + 1.33*S -(1.23e-2 * S * T) + (8.7e-5 * S * T*T)	// first order for small depths
        //
        // + (1.56e-2 * Z) + (2.55e-7 * Z*Z) -(7.3e-12 * Z*Z*Z)     // first order depth correction, max +4.70 m/s @ 300 m
        // + (1.2e-6 * Z * (Theta - 45))  -(9.5e-13 * T * Z*Z*Z)    // second order latitude/temperature/depth, max +/- .004 m/s @ 300m over latitude 0 to 90 degrees
        // + (3e-7 * T*T * Z) + (1.43e-5 * S * Z)                   // third order temperature/salinity/depth, max + .29 m/s @ 40°C, 35ppt, 300m

        let calculateSpeedOfSound(temperatureC : float<degC>,
                                  depthM : float<m>,
                                  salinity : float) : Double =

            let T = float temperatureC;
            let Z = float depthM;
            let S = salinity;

            1402.5 + (5.0 * T) - (5.44e-2 * T*T) + (2.1e-4 * T*T*T) + 1.33*S - (1.23e-2 * S * T) + (8.7e-5 * S * T*T) // first order for small depths
                + (1.56e-2 * Z) + (2.55e-7 * Z*Z) - (7.3e-12 * Z*Z*Z) // first order depth correction, max +4.70 m/s @ 300 m

        let internal validateDouble f name =

            if System.Double.IsNaN(f) then
                invalidArg name "is NaN"
            if System.Double.IsInfinity(f) then
                let flavor = if System.Double.IsPositiveInfinity(f) then
                                "+Infinity"
                             else
                                "-Infinity"
                invalidArg name ("is " + flavor)
