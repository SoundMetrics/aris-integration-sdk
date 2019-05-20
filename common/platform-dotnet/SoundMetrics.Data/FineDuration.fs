// Copyright 2011-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Data

open System

[<Struct>]
type FineDuration private (microseconds : float) =

    static member Zero = FineDuration 0.0
    static member OneMicrosecond = FineDuration 1.0

    static member FromMicroseconds (microseconds : float) = FineDuration microseconds
    static member FromMilliseconds (milliseconds : float) = FineDuration (milliseconds * 1000.0)

    member __.TotalMicroseconds = microseconds
    member __.TotalMilliseconds = microseconds / 1000.0
    member __.TotalSeconds = microseconds / 1000000.0

    member __.Floor = Math.Floor(microseconds)
    member __.Ceiling = Math.Ceiling(microseconds)

    override this.ToString() = String.Format("{0:0.000000} s", this.TotalSeconds)

    static member (/) (d : FineDuration, divisor) : FineDuration =
        FineDuration (d.TotalMicroseconds / divisor)
    static member (*) (d : FineDuration, multiplier) : FineDuration =
        FineDuration (d.TotalMicroseconds * multiplier)
    static member (*) (multiplier, d : FineDuration) : FineDuration =
        FineDuration (d.TotalMicroseconds * multiplier)

    static member (+) (d1 : FineDuration, d2 : FineDuration) : FineDuration =
        FineDuration (d1.TotalMicroseconds + d2.TotalMicroseconds)
    static member (-) (d : FineDuration, subtrahend : FineDuration) : FineDuration =
        FineDuration (d.TotalMicroseconds - subtrahend.TotalMicroseconds)

    // op_LessThan for interop with other CLI languages.
    static member op_LessThan(a : FineDuration, b : FineDuration) = a < b
    // op_GreaterThan for interop with other CLI languages.
    static member op_GreaterThan(a : FineDuration, b : FineDuration) = a > b
    // op_Equality for interop with other CLI languages.
    static member op_Equality(a : FineDuration, b : FineDuration) = (a = b)
    // op_Inequality for interop with other CLI languages.
    static member op_Inequality(a : FineDuration, b : FineDuration) = (a <> b)
    // op_GreaterThanOrEqual for interop with other CLI languages.
    static member op_GreaterThanOrEqual(a : FineDuration, b : FineDuration) = a >= b
    // op_LessThanOrEqual for interop with other CLI languages.
    static member op_LessThanOrEqual(a : FineDuration, b : FineDuration) = a <= b

    /// Returns the minimum of two values. C# implements Math.Min/Max as overloads
    /// rather than a comparable generic (dates to .NET 1.1, so before generics).
    /// F# implements min/max as comparable generics.
    /// (Extension methods have instance-method semantics, so aren't viable for
    /// extending Math.Min/Max.
    static member Min(a : FineDuration, b : FineDuration) =
        if a < b then a else b

    /// Returns the maximum of two values. C# implements Math.Min/Max as overloads
    /// rather than a comparable generic (dates to .NET 1.1, so before generics).
    /// F# implements min/max as comparable generics.
    /// (Extension methods have instance-method semantics, so aren't viable for
    /// extending Math.Min/Max.
    static member Max(a : FineDuration, b : FineDuration) =
        if a > b then a else b
