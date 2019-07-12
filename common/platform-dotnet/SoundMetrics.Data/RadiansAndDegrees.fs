namespace SoundMetrics.Data

open Trigonometry

[<Struct>]
type Radians (value: float) =

    member __.Value = value

    static member op_Explicit (value: float) = Radians(value)
    static member op_Explicit (d: Degrees) = Radians(degreesToRadians d.Value)

    static member op_UnaryNegation (r: Radians) = Radians(-r.Value)

    static member (+) (l: Radians, r: Radians) = Radians(l.Value + r.Value)
    static member (-) (l: Radians, r: Radians) = Radians(l.Value - r.Value)

    static member (/) (l: Radians, r: double) =     Radians(l.Value / r)
    static member (/) (l: Radians, r: single) =     Radians(l.Value / float r)
    static member (/) (l: Radians, r: int64) =      Radians(l.Value / float r)
    static member (/) (l: Radians, r: int32) =      Radians(l.Value / float r)
    static member (/) (l: Radians, r: int16) =      Radians(l.Value / float r)
    static member (/) (l: Radians, r: int8) =       Radians(l.Value / float r)
    static member (/) (l: Radians, r: uint64) =     Radians(l.Value / float r)
    static member (/) (l: Radians, r: uint32) =     Radians(l.Value / float r)
    static member (/) (l: Radians, r: uint16) =     Radians(l.Value / float r)
    static member (/) (l: Radians, r: uint8) =      Radians(l.Value / float r)
    static member (/) (l: Radians, r: decimal) =    Radians(l.Value / float r)

    static member (*) (l: Radians, r: double) =     Radians(l.Value * r)
    static member (*) (l: Radians, r: single) =     Radians(l.Value * float r)
    static member (*) (l: Radians, r: int64) =      Radians(l.Value * float r)
    static member (*) (l: Radians, r: int32) =      Radians(l.Value * float r)
    static member (*) (l: Radians, r: int16) =      Radians(l.Value * float r)
    static member (*) (l: Radians, r: int8) =       Radians(l.Value * float r)
    static member (*) (l: Radians, r: uint64) =     Radians(l.Value * float r)
    static member (*) (l: Radians, r: uint32) =     Radians(l.Value * float r)
    static member (*) (l: Radians, r: uint16) =     Radians(l.Value * float r)
    static member (*) (l: Radians, r: uint8) =      Radians(l.Value * float r)
    static member (*) (l: Radians, r: decimal) =    Radians(l.Value * float r)

    static member (*) (l: double, r: Radians) =     Radians(l * r.Value)
    static member (*) (l: single, r: Radians) =     Radians(float l * r.Value)
    static member (*) (l: int64, r: Radians) =      Radians(float l * r.Value)
    static member (*) (l: int32, r: Radians) =      Radians(float l * r.Value)
    static member (*) (l: int16, r: Radians) =      Radians(float l * r.Value)
    static member (*) (l: int8, r: Radians) =       Radians(float l * r.Value)
    static member (*) (l: uint64, r: Radians) =     Radians(float l * r.Value)
    static member (*) (l: uint32, r: Radians) =     Radians(float l * r.Value)
    static member (*) (l: uint16, r: Radians) =     Radians(float l * r.Value)
    static member (*) (l: uint8, r: Radians) =      Radians(float l * r.Value)
    static member (*) (l: decimal, r: Radians) =    Radians(float l * r.Value)

    static member op_LessThan (l: Radians, r: Radians) =            l.Value < r.Value
    static member op_LessThanOrEqual (l: Radians, r: Radians) =     l.Value <= r.Value
    static member op_GreaterThan (l: Radians, r: Radians) =         l.Value > r.Value
    static member op_GreaterThanOrEqual (l: Radians, r: Radians) =  l.Value >= r.Value

and [<Struct>] Degrees (value: float) =

    member __.Value = value

    static member op_Explicit (value: float) = Degrees(value)
    static member op_Explicit (r: Radians) = Degrees(radiansToDegrees r.Value)

    static member op_UnaryNegation (r: Degrees) = Degrees(-r.Value)

    static member (+) (l: Degrees, r: Degrees) = Degrees(l.Value + r.Value)
    static member (-) (l: Degrees, r: Degrees) = Degrees(l.Value - r.Value)

    static member (/) (l: Degrees, r: double) =     Degrees(l.Value / r)
    static member (/) (l: Degrees, r: single) =     Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: int64) =      Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: int32) =      Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: int16) =      Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: int8) =       Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: uint64) =     Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: uint32) =     Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: uint16) =     Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: uint8) =      Degrees(l.Value / float r)
    static member (/) (l: Degrees, r: decimal) =    Degrees(l.Value / float r)

    static member (*) (l: Degrees, r: double) =     Degrees(l.Value * r)
    static member (*) (l: Degrees, r: single) =     Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: int64) =      Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: int32) =      Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: int16) =      Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: int8) =       Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: uint64) =     Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: uint32) =     Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: uint16) =     Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: uint8) =      Degrees(l.Value * float r)
    static member (*) (l: Degrees, r: decimal) =    Degrees(l.Value * float r)

    static member (*) (l: double, r: Degrees) =     Degrees(l * r.Value)
    static member (*) (l: single, r: Degrees) =     Degrees(float l * r.Value)
    static member (*) (l: int64, r: Degrees) =      Degrees(float l * r.Value)
    static member (*) (l: int32, r: Degrees) =      Degrees(float l * r.Value)
    static member (*) (l: int16, r: Degrees) =      Degrees(float l * r.Value)
    static member (*) (l: int8, r: Degrees) =       Degrees(float l * r.Value)
    static member (*) (l: uint64, r: Degrees) =     Degrees(float l * r.Value)
    static member (*) (l: uint32, r: Degrees) =     Degrees(float l * r.Value)
    static member (*) (l: uint16, r: Degrees) =     Degrees(float l * r.Value)
    static member (*) (l: uint8, r: Degrees) =      Degrees(float l * r.Value)
    static member (*) (l: decimal, r: Degrees) =    Degrees(float l * r.Value)

    static member op_LessThan (l: Degrees, r: Degrees) =            l.Value < r.Value
    static member op_LessThanOrEqual (l: Degrees, r: Degrees) =     l.Value <= r.Value
    static member op_GreaterThan (l: Degrees, r: Degrees) =         l.Value > r.Value
    static member op_GreaterThanOrEqual (l: Degrees, r: Degrees) =  l.Value >= r.Value
