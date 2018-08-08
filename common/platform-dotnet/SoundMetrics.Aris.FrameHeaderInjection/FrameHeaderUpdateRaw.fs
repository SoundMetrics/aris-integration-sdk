// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.FrameHeaderInjection

open System
open System.Runtime.InteropServices

// warning FS0009: Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn "9"'.
//#nowarn "9"

[<Struct>]
[<StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type internal FrameHeaderInfoUpdateRaw =
    val mutable FVelocity :         float32
    val mutable FDepth :            float32
    val mutable FAltitude :         float32
    val mutable FPitch :            float32
    val mutable FPitchRate :        float32
    val mutable FRoll :             float32
    val mutable FRollRate :         float32
    val mutable FHeading :          float32
    val mutable FHeadingRate :      float32
    val mutable FSonarPan :         float32
    val mutable FSonarTilt :        float32
    val mutable FSonarRoll :        float32
    val mutable DLatitude :         double
    val mutable DLongitude :        double
    val mutable FSonarPosition :    float32
    val mutable FTargetRange :      float32
    val mutable FTargetBearing :    float32
    val mutable BTargetPresent :    uint32
    val mutable UpdateFlags :       uint32
    val mutable FUser1 :            float32
    val mutable FUser2 :            float32
    val mutable FUser3 :            float32
    val mutable FUser4 :            float32
    val mutable FUser5 :            float32
    val mutable FUser6 :            float32
    val mutable FUser7 :            float32
    val mutable FUser8 :            float32
    val mutable NDegC2 :            uint32
    val mutable NFrameNumber :      uint32
    val mutable FWaterTemp :        float32
    val mutable FSonarX :           float32
    val mutable FSonarY :           float32
    val mutable FSonarZ :           float32
    val mutable FSonarPanOffset :   float32
    val mutable FSonarTiltOffset :  float32
    val mutable FSonarRollOffset :  float32
    val mutable DVehicleTime :      double

module private HeaderInfoUpdateRawImpl =
    [<Flags>]
    type HeaderUpdateFlag =
        | UPDATE_VELOCITY      = 0x00000001u
        | UPDATE_DEPTH         = 0x00000002u
        | UPDATE_ALTITUDE      = 0x00000004u
        | UPDATE_PITCH         = 0x00000008u
        | UPDATE_PITCHRATE     = 0x00000010u
        | UPDATE_ROLL          = 0x00000020u
        | UPDATE_ROLLRATE      = 0x00000040u
        | UPDATE_HEADING       = 0x00000080u
        | UPDATE_HEADINGRATE   = 0x00000100u
        | UPDATE_SONARPAN      = 0x00000200u
        | UPDATE_SONARTILT     = 0x00000400u
        | UPDATE_SONARROLL     = 0x00000800u
        | UPDATE_LATITUDE      = 0x00001000u
        | UPDATE_LONGITUDE     = 0x00002000u
        | UPDATE_SONARPOSITION = 0x00004000u
        | UPDATE_TARGETRANGE   = 0x00008000u
        | UPDATE_TARGETBEARING = 0x00010000u
        | UPDATE_TARGETPRESENT = 0x00020000u
        | UPDATE_USERDATA      = 0x00040000u
        | UPDATE_SONARTIME     = 0x00080000u
        | UPDATE_DEGC2         = 0x00100000u
        | UPDATE_FRAMENUMBER   = 0x00200000u
        | UPDATE_WATERTEMP     = 0x00400000u
        | UPDATE_SONARX        = 0x00800000u
        | UPDATE_SONARY        = 0x01000000u
        | UPDATE_SONARZ        = 0x02000000u
        | UPDATE_VEHICLETIME   = 0x04000000u
        | UPDATE_GGK           = 0x08000000u
        | UPDATE_PANOFFSET     = 0x10000000u
        | UPDATE_TILTOFFSET    = 0x20000000u
        | UPDATE_ROLLOFFSET    = 0x40000000u
        | UPDATE_GPSPOSTIME    = 0x80000000u

    let updateField (flags : uint32 byref) (field : _ byref) (value : _ Nullable) flag =

        if value.HasValue then
            flags <- flags ||| uint32 flag
            field <- value.Value

    //let setFlagForValue<'T> (bits: uint32 ref) (t: _ Nullable) flag =
    //    if t.HasValue then
    //        bits := !bits ||| uint32 flag ; true
    //    else
    //        false

    //let setFlagForValues<'T> (bits: uint32 ref) (values: 'T option list) flag =

    //    // Check whether all values are available
    //    if values |> List.forall (fun v -> v.IsSome) then
    //        bits := !bits ||| uint32 flag
    //        true
    //    else
    //        false

    let updateTime (flags : uint32 byref)
                   (u : FrameHeaderInfoUpdateRaw byref)
                   (value : Nullable<HeaderUpdateSonarTime>)
                   flag =

        if value.HasValue then
            flags <- flags ||| uint32 flag

            let time = value.Value
            u.FUser1 <- float32 time.Year
            u.FUser2 <- float32 time.Month
            u.FUser3 <- float32 time.Day
            u.FUser4 <- float32 time.Hour
            u.FUser5 <- float32 time.Minute
            u.FUser6 <- float32 time.Second
            u.FUser7 <- float32 time.HSecond

    let updateGGK (flags : uint32 byref)
                  (u : FrameHeaderInfoUpdateRaw byref)
                  (value : Nullable<HeaderUpdateGGK>)
                  flag =


        if value.HasValue then
            flags <- flags ||| uint32 flag

            let ggk = value.Value
            u.FUser1 <- float32 ggk.TimeGGK
            u.FUser2 <- float32 ggk.DateGGK
            u.FUser3 <- float32 ggk.QualityGGK
            u.FUser4 <- float32 ggk.NumSatsGGK
            u.FUser5 <- float32 ggk.DOPGGK
            u.FUser6 <- float32 ggk.EHTGGK

open HeaderInfoUpdateRawImpl

type FrameHeaderInfoUpdateRaw with
    static member From (update : HeaderInfoUpdate) =

        let mutable u = FrameHeaderInfoUpdateRaw()
        let mutable flags = 0u

        updateField &flags &u.FVelocity         update.FVelocity        HeaderUpdateFlag.UPDATE_VELOCITY
        updateField &flags &u.FDepth            update.FDepth           HeaderUpdateFlag.UPDATE_DEPTH
        updateField &flags &u.FAltitude         update.FAltitude        HeaderUpdateFlag.UPDATE_ALTITUDE
        updateField &flags &u.FPitch            update.FPitch           HeaderUpdateFlag.UPDATE_PITCH
        updateField &flags &u.FPitchRate        update.FPitchRate       HeaderUpdateFlag.UPDATE_PITCHRATE
        updateField &flags &u.FRoll             update.FRoll            HeaderUpdateFlag.UPDATE_ROLL
        updateField &flags &u.FRollRate         update.FRollRate        HeaderUpdateFlag.UPDATE_ROLLRATE
        updateField &flags &u.FHeading          update.FHeading         HeaderUpdateFlag.UPDATE_HEADING
        updateField &flags &u.FHeadingRate      update.FHeadingRate     HeaderUpdateFlag.UPDATE_HEADINGRATE
        updateField &flags &u.FSonarPan         update.FSonarPan        HeaderUpdateFlag.UPDATE_SONARPAN
        updateField &flags &u.FSonarTilt        update.FSonarTilt       HeaderUpdateFlag.UPDATE_SONARTILT
        updateField &flags &u.FSonarRoll        update.FSonarRoll       HeaderUpdateFlag.UPDATE_SONARROLL
        updateField &flags &u.DLatitude         update.DLatitude        HeaderUpdateFlag.UPDATE_LATITUDE
        updateField &flags &u.DLongitude        update.DLongitude       HeaderUpdateFlag.UPDATE_LONGITUDE
        updateField &flags &u.FSonarPosition    update.FSonarPosition   HeaderUpdateFlag.UPDATE_SONARPOSITION
        updateField &flags &u.FTargetRange      update.FTargetRange     HeaderUpdateFlag.UPDATE_TARGETRANGE
        updateField &flags &u.FTargetBearing    update.FTargetBearing   HeaderUpdateFlag.UPDATE_TARGETBEARING
        updateField &flags &u.BTargetPresent    update.BTargetPresent   HeaderUpdateFlag.UPDATE_TARGETPRESENT
        updateField &flags &u.FUser1            update.FUser1           HeaderUpdateFlag.UPDATE_USERDATA
        updateField &flags &u.FUser2            update.FUser2           HeaderUpdateFlag.UPDATE_USERDATA
        updateField &flags &u.FUser3            update.FUser3           HeaderUpdateFlag.UPDATE_USERDATA
        updateField &flags &u.FUser4            update.FUser4           HeaderUpdateFlag.UPDATE_USERDATA
        updateField &flags &u.FUser5            update.FUser5           HeaderUpdateFlag.UPDATE_USERDATA
        updateField &flags &u.FUser6            update.FUser6           HeaderUpdateFlag.UPDATE_USERDATA
        updateField &flags &u.FUser7            update.FUser7           HeaderUpdateFlag.UPDATE_USERDATA
        updateField &flags &u.FUser8            update.FUser8           HeaderUpdateFlag.UPDATE_USERDATA
        updateField &flags &u.NDegC2            update.NDegC2           HeaderUpdateFlag.UPDATE_DEGC2
        updateField &flags &u.NFrameNumber      update.NFrameNumber     HeaderUpdateFlag.UPDATE_FRAMENUMBER
        updateField &flags &u.FWaterTemp        update.FWaterTemp       HeaderUpdateFlag.UPDATE_WATERTEMP
        updateField &flags &u.FSonarX           update.FSonarX          HeaderUpdateFlag.UPDATE_SONARX
        updateField &flags &u.FSonarY           update.FSonarY          HeaderUpdateFlag.UPDATE_SONARY
        updateField &flags &u.FSonarZ           update.FSonarZ          HeaderUpdateFlag.UPDATE_SONARZ
        updateField &flags &u.FSonarPanOffset   update.FSonarPanOffset  HeaderUpdateFlag.UPDATE_PANOFFSET
        updateField &flags &u.FSonarTiltOffset  update.FSonarTiltOffset HeaderUpdateFlag.UPDATE_TILTOFFSET
        updateField &flags &u.FSonarRollOffset  update.FSonarRollOffset HeaderUpdateFlag.UPDATE_ROLLOFFSET
        updateField &flags &u.DVehicleTime      update.DVehicleTime     HeaderUpdateFlag.UPDATE_VEHICLETIME

        updateTime  &flags &u                   update.SonarTime        HeaderUpdateFlag.UPDATE_SONARTIME
        updateGGK   &flags &u                   update.Ggk              HeaderUpdateFlag.UPDATE_GGK

        updateField &flags &u.DVehicleTime      update.DVehicleTime     HeaderUpdateFlag.UPDATE_GPSPOSTIME
        updateField &flags &u.DLatitude         update.DLatitude        HeaderUpdateFlag.UPDATE_GPSPOSTIME
        updateField &flags &u.DLongitude        update.DLongitude       HeaderUpdateFlag.UPDATE_GPSPOSTIME

        u.UpdateFlags <- flags
        u
