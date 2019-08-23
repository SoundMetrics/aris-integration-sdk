namespace SoundMetrics.Aris.Comms.Experimental

open Aris.FileTypes
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open SoundMetrics.Aris.AcousticSettings
open System

/// Provides bindable properties for ArisFrameHeader.
[<Sealed>]
type ArisFrameHeaderBindable public (header: ArisFrameHeader) =

    /// Gets the entire ArisFrameHeader.
    member public __.EntireHeader = header

    member public __.BeamCount =
        (SonarConfig.getPingModeConfig (PingMode.From(header.PingMode)))
            .ChannelCount

    member public __.Window : DownrangeWindow =
        let window =
            let floatToOption x =
                if Double.IsNaN(x) then
                    None
                else
                    Some x

            let waterTemp =
                let wt = floatToOption (float header.WaterTemp)
                defaultArg wt 15.0
            let depth =
                let d = floatToOption (float header.WaterTemp)
                defaultArg d 0.0

            AcousticMath.CalculateWindow(
                int header.SampleStartDelay * 1<Us>,
                int header.SamplePeriod * 1<Us>,
                int header.SamplesPerBeam,
                waterTemp * 1.0<degC>,
                depth * 1.0<m>,
                float header.Salinity)
        window

    member public hdr.FocusRange : float<m> = hdr.Window.MidPoint

    /// Frame number in file
    member public __.FrameIndex = header.FrameIndex

    /// PC time stamp when recorded; microseconds since epoch (Jan 1st 1970)
    member public __.FrameTime = header.FrameTime

    /// ARIS file format version = 0x05464444
    member public __.Version = header.Version

    member public __.Status = header.Status

    /// On-sonar microseconds since epoch (Jan 1st 1970)
    member public __.SonarTimeStamp = header.sonarTimeStamp

    member public __.TS_Day = header.TS_Day

    member public __.TS_Hour = header.TS_Hour

    member public __.TS_Minute = header.TS_Minute

    member public __.TS_Second = header.TS_Second

    member public __.TS_Hsecond = header.TS_Hsecond

    member public __.TransmitMode = header.TransmitMode

    /// Window start in meters
    member public __.WindowStart = header.WindowStart

    /// Window length in meters
    member public __.WindowLength = header.WindowLength

    member public __.Threshold = header.Threshold

    member public __.Intensity = header.Intensity

    /// Note: 0-24 dB
    member public __.ReceiverGain = header.ReceiverGain

    /// CPU temperature
    /// Note: Celsius
    member public __.DegC1 = header.DegC1

    /// Power supply temperature
    /// Note: Celsius
    member public __.DegC2 = header.DegC2

    /// % relative humidity
    member public __.Humidity = header.Humidity

    /// Focus units 0-1000
    member public __.Focus = header.Focus

    member public __.UserValue1 = header.UserValue1

    member public __.UserValue2 = header.UserValue2

    member public __.UserValue3 = header.UserValue3

    member public __.UserValue4 = header.UserValue4

    member public __.UserValue5 = header.UserValue5

    member public __.UserValue6 = header.UserValue6

    member public __.UserValue7 = header.UserValue7

    member public __.UserValue8 = header.UserValue8

    /// Platform velocity from AUV integration
    member public __.Velocity = header.Velocity

    /// Platform depth from AUV integration
    member public __.Depth = header.Depth

    /// Platform altitude from AUV integration
    member public __.Altitude = header.Altitude

    /// Platform pitch from AUV integration
    member public __.Pitch = header.Pitch

    /// Platform pitch rate from AUV integration
    member public __.PitchRate = header.PitchRate

    /// Platform roll from AUV integration
    member public __.Roll = header.Roll

    /// Platform roll rate from AUV integration
    member public __.RollRate = header.RollRate

    /// Platform heading from AUV integration
    member public __.Heading = header.Heading

    /// Platform heading rate from AUV integration
    member public __.HeadingRate = header.HeadingRate

    /// Sonar compass heading output
    member public __.CompassHeading = header.CompassHeading

    /// Sonar compass pitch output
    member public __.CompassPitch = header.CompassPitch

    /// Sonar compass roll output
    member public __.CompassRoll = header.CompassRoll

    /// from auxiliary GPS sensor
    member public __.Latitude = header.Latitude

    /// from auxiliary GPS sensor
    member public __.Longitude = header.Longitude

    /// Note: special for PNNL
    member public __.SonarPosition = header.SonarPosition

    member public __.ConfigFlags = header.ConfigFlags

    member public __.BeamTilt = header.BeamTilt

    member public __.TargetRange = header.TargetRange

    member public __.TargetBearing = header.TargetBearing

    member public __.TargetPresent = header.TargetPresent

    member public __.Flags = header.Flags

    /// Source file frame number for CSOT output files
    member public __.SourceFrame = header.SourceFrame

    /// Water temperature from housing temperature sensor
    member public __.WaterTemp = header.WaterTemp

    member public __.TimerPeriod = header.TimerPeriod

    /// Sonar X location for 3D processing
    /// Note: Bluefin, external sensor data
    member public __.SonarX = header.SonarX

    /// Sonar Y location for 3D processing
    member public __.SonarY = header.SonarY

    /// Sonar Z location for 3D processing
    member public __.SonarZ = header.SonarZ

    /// X2 pan output
    member public __.SonarPan = header.SonarPan

    /// X2 tilt output
    member public __.SonarTilt = header.SonarTilt

    /// X2 roll output
    member public __.SonarRoll = header.SonarRoll

    member public __.PanPNNL = header.PanPNNL

    member public __.TiltPNNL = header.TiltPNNL

    member public __.RollPNNL = header.RollPNNL

    /// Note: special for Bluefin HAUV or other AUV integration
    member public __.VehicleTime = header.VehicleTime

    /// GPS output from NMEA GGK message
    member public __.TimeGGK = header.TimeGGK

    /// GPS output from NMEA GGK message
    member public __.DateGGK = header.DateGGK

    /// GPS output from NMEA GGK message
    member public __.QualityGGK = header.QualityGGK

    /// GPS output from NMEA GGK message
    member public __.NumSatsGGK = header.NumSatsGGK

    /// GPS output from NMEA GGK message
    member public __.DOPGGK = header.DOPGGK

    /// GPS output from NMEA GGK message
    member public __.EHTGGK = header.EHTGGK

    /// external sensor
    member public __.HeaveTSS = header.HeaveTSS

    /// GPS year output
    member public __.YearGPS = header.YearGPS

    /// GPS month output
    member public __.MonthGPS = header.MonthGPS

    /// GPS day output
    member public __.DayGPS = header.DayGPS

    /// GPS hour output
    member public __.HourGPS = header.HourGPS

    /// GPS minute output
    member public __.MinuteGPS = header.MinuteGPS

    /// GPS second output
    member public __.SecondGPS = header.SecondGPS

    /// GPS 1/100th second output
    member public __.HSecondGPS = header.HSecondGPS

    /// Sonar mount location pan offset for 3D processing = header meters
    member public __.SonarPanOffset = header.SonarPanOffset

    /// Sonar mount location tilt offset for 3D processing
    member public __.SonarTiltOffset = header.SonarTiltOffset

    /// Sonar mount location roll offset for 3D processing
    member public __.SonarRollOffset = header.SonarRollOffset

    /// Sonar mount location X offset for 3D processing
    member public __.SonarXOffset = header.SonarXOffset

    /// Sonar mount location Y offset for 3D processing
    member public __.SonarYOffset = header.SonarYOffset

    /// Sonar mount location Z offset for 3D processing
    member public __.SonarZOffset = header.SonarZOffset

    /// 3D processing transformation matrix
    member public __.Tmatrix = header.Tmatrix

    /// Calculated as 1e6/SamplePeriod
    member public __.SampleRate = header.SampleRate

    /// X-axis sonar acceleration
    member public __.AccellX = header.AccellX

    /// Y-axis sonar acceleration
    member public __.AccellY = header.AccellY

    /// Z-axis sonar acceleration
    member public __.AccellZ = header.AccellZ

    /// ARIS ping mode
    /// Note: 1..12
    member public __.PingMode = header.PingMode

    /// Frequency
    /// Note: 1 = HF, 0 = LF
    member public __.FrequencyHiLow = header.FrequencyHiLow

    /// Width of transmit pulse
    /// Note: 4..100 microseconds
    member public __.PulseWidth = header.PulseWidth

    /// Ping cycle time
    /// Note: 1802..65535 microseconds
    member public __.CyclePeriod = header.CyclePeriod

    /// Downrange sample rate
    /// Note: 4..100 microseconds
    member public __.SamplePeriod = header.SamplePeriod

    /// 1 = Transmit ON, 0 = Transmit OFF
    member public __.TransmitEnable = header.TransmitEnable

    /// Instantaneous frame rate between frame N and frame N-1
    /// Note: microseconds
    member public __.FrameRate = header.FrameRate

    /// Sound velocity in water calculated from water temperature depth and salinity setting
    /// Note: m/s
    member public __.SoundSpeed = header.SoundSpeed

    /// Number of downrange samples in each beam
    member public __.SamplesPerBeam = header.SamplesPerBeam

    /// 1 = 150V ON (Max Power), 0 = 150V OFF (Min Power, 12V)
    member public __.Enable150V = header.Enable150V

    /// Delay from transmit until start of sampling (window start) in usec, [930..65535]
    member public __.SampleStartDelay = header.SampleStartDelay

    /// 1 = telephoto lens (large lens, big lens, hi-res lens) present
    member public __.LargeLens = header.LargeLens

    /// 1 = ARIS 3000, 0 = ARIS 1800, 2 = ARIS 1200
    member public __.TheSystemType = header.TheSystemType

    /// Sonar serial number as labeled on housing
    member public __.SonarSerialNumber = header.SonarSerialNumber

    /// Error flag code bits
    member public __.ArisErrorFlags = header.ArisErrorFlagsUint

    /// Missed packet count for Ethernet statistics reporting
    member public __.MissedPackets = header.MissedPackets

    /// Version number of ArisApp sending frame data
    member public __.ArisAppVersion = header.ArisAppVersion

    /// 1 = frame data already ordered into [beam,sample] array, 0 = needs reordering
    member public __.ReorderedSamples = header.ReorderedSamples

    /// Water salinity code:  0 = fresh, 15 = brackish, 35 = salt
    member public __.Salinity = header.Salinity

    /// Depth sensor output
    /// Note: psi
    member public __.Pressure = header.Pressure

    /// Battery input voltage before power steering
    /// Note: mV
    member public __.BatteryVoltage = header.BatteryVoltage

    /// Main cable input voltage before power steering
    /// Note: mV
    member public __.MainVoltage = header.MainVoltage

    /// Input voltage after power steering = header filtered voltage
    /// Note: mV
    member public __.SwitchVoltage = header.SwitchVoltage

    /// Note: Added 14-Aug-2012 for AutomaticRecording
    member public __.FocusMotorMoving = header.FocusMotorMoving

    /// Note: Added 16-Aug (first two bits = 12V, second two bits = 150V, 00 = not changing, 01 = turning on, 10 = turning off)
    member public __.VoltageChanging = header.VoltageChanging

    member public __.FocusTimeoutFault = header.FocusTimeoutFault

    member public __.FocusOverCurrentFault = header.FocusOverCurrentFault

    member public __.FocusNotFoundFault = header.FocusNotFoundFault

    member public __.FocusStalledFault = header.FocusStalledFault

    member public __.FPGATimeoutFault = header.FPGATimeoutFault

    member public __.FPGABusyFault = header.FPGABusyFault

    member public __.FPGAStuckFault = header.FPGAStuckFault

    member public __.CPUTempFault = header.CPUTempFault

    member public __.PSUTempFault = header.PSUTempFault

    member public __.WaterTempFault = header.WaterTempFault

    member public __.HumidityFault = header.HumidityFault

    member public __.PressureFault = header.PressureFault

    member public __.VoltageReadFault = header.VoltageReadFault

    member public __.VoltageWriteFault = header.VoltageWriteFault

    /// Focus shaft current position
    /// Note: 0..1000 motor units
    member public __.FocusCurrentPosition = header.FocusCurrentPosition

    /// Commanded pan position
    member public __.TargetPan = header.TargetPan

    /// Commanded tilt position
    member public __.TargetTilt = header.TargetTilt

    /// Commanded roll position
    member public __.TargetRoll = header.TargetRoll

    member public __.PanMotorErrorCode = header.PanMotorErrorCode

    member public __.TiltMotorErrorCode = header.TiltMotorErrorCode

    member public __.RollMotorErrorCode = header.RollMotorErrorCode

    /// Low-resolution magnetic encoder absolute pan position (NaN indicates no arm detected for axis since 2.6.0.8403)
    member public __.PanAbsPosition = header.PanAbsPosition

    /// Low-resolution magnetic encoder absolute tilt position (NaN indicates no arm detected for axis since 2.6.0.8403)
    member public __.TiltAbsPosition = header.TiltAbsPosition

    /// Low-resolution magnetic encoder absolute roll position (NaN indicates no arm detected for axis since 2.6.0.8403)
    member public __.RollAbsPosition = header.RollAbsPosition

    /// Accelerometer outputs from AR2 CPU board sensor
    /// Note: G
    member public __.PanAccelX = header.PanAccelX

    /// Note: G
    member public __.PanAccelY = header.PanAccelY

    /// Note: G
    member public __.PanAccelZ = header.PanAccelZ

    /// Note: G
    member public __.TiltAccelX = header.TiltAccelX

    /// Note: G
    member public __.TiltAccelY = header.TiltAccelY

    /// Note: G
    member public __.TiltAccelZ = header.TiltAccelZ

    /// Note: G
    member public __.RollAccelX = header.RollAccelX

    /// Note: G
    member public __.RollAccelY = header.RollAccelY

    /// Note: G
    member public __.RollAccelZ = header.RollAccelZ

    /// Cookie indices for command acknowlege in frame header
    member public __.AppliedSettings = header.AppliedSettings

    /// Cookie indices for command acknowlege in frame header
    member public __.ConstrainedSettings = header.ConstrainedSettings

    /// Cookie indices for command acknowlege in frame header
    member public __.InvalidSettings = header.InvalidSettings

    /// If true delay is added between sending out image data packets
    member public __.EnableInterpacketDelay = header.EnableInterpacketDelay

    /// packet delay factor in us (does not include function overhead time)
    member public __.InterpacketDelayPeriod = header.InterpacketDelayPeriod

    /// Total time the sonar has been running over its lifetime.
    /// Note: seconds
    member public __.Uptime = header.Uptime

    /// Major version number
    member public __.ArisAppVersionMajor = header.ArisAppVersionMajor

    /// Minor version number
    member public __.ArisAppVersionMinor = header.ArisAppVersionMinor

    /// Sonar time when frame cycle is initiated in hardware
    member public __.GoTime = header.GoTime

    /// AR2 pan velocity
    /// Note: degrees/second
    member public __.PanVelocity = header.PanVelocity

    /// AR2 tilt velocity
    /// Note: degrees/second
    member public __.TiltVelocity = header.TiltVelocity

    /// AR2 roll velocity
    /// Note: degrees/second
    member public __.RollVelocity = header.RollVelocity

    /// Age of the last GPS fix acquired = header capped at 0xFFFFFFFF = header zero if none
    /// Note: microseconds
    member public __.GpsTimeAge = header.GpsTimeAge

    /// bit 0 = Defender
    member public __.SystemVariant = header.SystemVariant
