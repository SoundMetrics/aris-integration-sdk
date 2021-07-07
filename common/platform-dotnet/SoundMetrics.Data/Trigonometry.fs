namespace SoundMetrics.Data

module Trigonometry =

    let RadiansPerDegree = System.Math.PI / 180.0
    let DegreesPerRadian = 180.0 / System.Math.PI

    [<CompiledName("DegreesToRadians")>]
    let degreesToRadians (d: float) = d * RadiansPerDegree

    [<CompiledName("RadiansToDegrees")>]
    let radiansToDegrees (r: float) = r * DegreesPerRadian
