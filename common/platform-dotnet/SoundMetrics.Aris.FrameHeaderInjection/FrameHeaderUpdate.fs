// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.FrameHeaderInjection

open System

[<Struct>]
type HeaderUpdateSonarTime = {
    Year :      uint32
    Month :     uint32
    Day :       uint32
    Hour :      uint32
    Minute :    uint32
    Second :    uint32
    HSecond :   uint32
}

[<Struct>]
type HeaderUpdateGGK = {
    TimeGGK :       float32
    DateGGK :       uint32
    QualityGGK :    uint32
    NumSatsGGK :    uint32
    DOPGGK :        float32
    EHTGGK :        float32
}

type HeaderInfoUpdate = {
    /// m/s
    FVelocity :         Nullable<float32>
    FDepth :            Nullable<float32>
    FAltitude :         Nullable<float32>
    FPitch :            Nullable<float32>
    FPitchRate :        Nullable<float32>
    FRoll :             Nullable<float32>
    FRollRate :         Nullable<float32>
    FHeading :          Nullable<float32>
    FHeadingRate :      Nullable<float32>
    FSonarPan :         Nullable<float32>
    FSonarTilt :        Nullable<float32>
    FSonarRoll :        Nullable<float32>
    DLatitude :         Nullable<double>
    DLongitude :        Nullable<double>
    FSonarPosition :    Nullable<float32>
    FTargetRange :      Nullable<float32>
    FTargetBearing :    Nullable<float32>
    BTargetPresent :    Nullable<uint32>
    FUser1 :            Nullable<float32>
    FUser2 :            Nullable<float32>
    FUser3 :            Nullable<float32>
    FUser4 :            Nullable<float32>
    FUser5 :            Nullable<float32>
    FUser6 :            Nullable<float32>
    FUser7 :            Nullable<float32>
    FUser8 :            Nullable<float32>
    NDegC2 :            Nullable<uint32>
    NFrameNumber :      Nullable<uint32>
    FWaterTemp :        Nullable<float32>
    FSonarX :           Nullable<float32>
    FSonarY :           Nullable<float32>
    FSonarZ :           Nullable<float32>
    FSonarPanOffset :   Nullable<float32>
    FSonarTiltOffset :  Nullable<float32>
    FSonarRollOffset :  Nullable<float32>
    DVehicleTime :      Nullable<double>
    SonarTime :         Nullable<HeaderUpdateSonarTime>
    Ggk :               Nullable<HeaderUpdateGGK>
}

module private HeaderUpdateDetails =
    let empty = lazy (
        {
            FVelocity =         Nullable<_>()
            FDepth =            Nullable<_>()
            FAltitude =         Nullable<_>()
            FPitch =            Nullable<_>()
            FPitchRate =        Nullable<_>()
            FRoll =             Nullable<_>()
            FRollRate =         Nullable<_>()
            FHeading =          Nullable<_>()
            FHeadingRate =      Nullable<_>()
            FSonarPan =         Nullable<_>()
            FSonarTilt =        Nullable<_>()
            FSonarRoll =        Nullable<_>()
            DLatitude =         Nullable<_>()
            DLongitude =        Nullable<_>()
            FSonarPosition =    Nullable<_>()
            FTargetRange =      Nullable<_>()
            FTargetBearing =    Nullable<_>()
            BTargetPresent =    Nullable<_>()
            FUser1 =            Nullable<_>()
            FUser2 =            Nullable<_>()
            FUser3 =            Nullable<_>()
            FUser4 =            Nullable<_>()
            FUser5 =            Nullable<_>()
            FUser6 =            Nullable<_>()
            FUser7 =            Nullable<_>()
            FUser8 =            Nullable<_>()
            NDegC2 =            Nullable<_>()
            NFrameNumber =      Nullable<_>()
            FWaterTemp =        Nullable<_>()
            FSonarX =           Nullable<_>()
            FSonarY =           Nullable<_>()
            FSonarZ =           Nullable<_>()
            FSonarPanOffset =   Nullable<_>()
            FSonarTiltOffset =  Nullable<_>()
            FSonarRollOffset =  Nullable<_>()
            DVehicleTime =      Nullable<_>()
            SonarTime =         Nullable<_>()
            Ggk =               Nullable<_>()
        })

    [<Literal>]
    let ticksPerSec = 10000000.0 // 1000 * 1000 * 1000 / 100

    let epoch = System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)

type HeaderInfoUpdate with
    static member Empty = HeaderUpdateDetails.empty.Value

    static member DateTimeToVehicleTime (dt: System.DateTime) =

        // Convert ticks (100-nanoseconds each since 0001-01-01 0:0:0 UTC) to
        // Linux seconds since epoch (1970-01-01 0:0:0 UTC).

        // Don't do math resulting in TimeSpan here, it rounds off seconds.
        let ticksSinceEpoch =   dt.Ticks - HeaderUpdateDetails.epoch.Ticks
        let seconds = float ticksSinceEpoch / HeaderUpdateDetails.ticksPerSec
        seconds
