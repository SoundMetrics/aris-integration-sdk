// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.AcousticSettings.Experimental

type DeviceSettings = {
    PingMode:   int
}

type AuxLensType = None | Telephoto
type Salinity = Fresh = 0 | Brackish = 15 | Saltwater = 35

type ExternalContext = {
    WaterTemp:  float32
    AuxLens:    AuxLensType
    Salinity:   Salinity
}

type ProjectionToSettings<'P> = ExternalContext -> 'P -> DeviceSettings

type ProjectionMap<'P,'C> = {
    Constrain:  'P -> 'P
    Change:     'P -> 'C -> 'P
    ToSettings: ProjectionToSettings<'P>
}

//-----------------------------------------------------------------------------

type ComputedValues = {
    Resolution:     float32
    AutoFocusRange: float32
    SoundSpeed:     float32
}


module SettingsProjections =

    let private getComputedValues extCtx (deviceSettings: DeviceSettings) =
        // TODO
        { Resolution = 6.9f; AutoFocusRange = 4.0f; SoundSpeed = 1500.0f }

    //-----------------------------------------------------------------------------

    let private normalize (settings : DeviceSettings) =

        failwith "nyi"

    //-----------------------------------------------------------------------------

    [<CompiledName("ChangeProjection")>]
    let changeProjection<'P,'C> (pmap: ProjectionMap<'P,'C>)
                                (projection: 'P)
                                (changes: 'C list)
                                externalContext
                                : struct ('P * DeviceSettings * ComputedValues) =

        let constrainedProjection =
            changes |> List.fold pmap.Change projection // Apply any changes
                    |> pmap.Constrain
        let deviceSettings = pmap.ToSettings externalContext constrainedProjection
                                |> normalize
        let computedValues = deviceSettings |> getComputedValues externalContext
        struct (constrainedProjection, deviceSettings, computedValues)
