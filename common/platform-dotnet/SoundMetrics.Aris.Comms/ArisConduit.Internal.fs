// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.Config
open SoundMetrics.Aris.Comms
open System
open System.Net
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading.Tasks

module internal ArisConduitHelpers =

    type ParsedTargetId =
    | SN of sn: SerialNumber
    | IP of ip: IPAddress
    | Unknown of msg: string

    /// Contains the context required to correctly set focus. Each of the fields affects focus.
    /// You should generally get this context from a frame received from the sonar.
    type FocusContext = {
        SystemType: SystemType
        Temperature: float<degC>
        Depth: float<m>
        Salinity: Salinity
        Telephoto: bool
        ObservedFocus: FU }
    with
        static member FromFrame (fr : Frame) =
            // Use 0 if depth is not available from the frame.
            let depth =
                let fd = fr.Header.Depth
                if Double.IsNaN(float fd) || Double.IsInfinity(float fd) then
                    0.0<m>
                else
                    float fd * 1.0<m>

            { SystemType = enum (int fr.Header.TheSystemType)
              Temperature = float fr.Header.WaterTemp * 1.0<degC>
              Depth = float depth * 1.0<m>
              Salinity = enum (int fr.Header.Salinity)
              Telephoto = (fr.Header.LargeLens = 1u)
              ObservedFocus = fr.Header.FocusCurrentPosition }

        override fc.ToString() = sprintf "%A" fc


    [<CompiledName("TargetIdFromStringAsync")>]
    let targetIdFromStringAsync s: Task<ParsedTargetId> =
        try
            let parsed, sn = UInt32.TryParse(s)
            if parsed then
                Task.FromResult (SN (int sn))
            else
                let parsed, ip = IPAddress.TryParse(s)
                if parsed then
                    Task.FromResult (IP ip)
                else
                    Task.Run (fun  () ->
                        try
                            let hostEntry = Dns.GetHostEntry(s)
                            IP hostEntry.AddressList.[0]
                        with
                        | ex -> let msg = sprintf "Exception: couldn't resolve '%s' %s %s" s ex.Message ex.StackTrace
                                System.Diagnostics.Trace.TraceError(msg)
                                System.Diagnostics.Debug.WriteLine(msg)
                                Unknown (sprintf "Error: couldn't resolve '%s' %s" s ex.Message)
                        )
        with
            | ex -> Task.FromResult (Unknown (sprintf "Error: %s" ex.Message))


module internal SonarConduitDetails =
    open System.Diagnostics
    open System.Net.NetworkInformation

    let logNewSonarConduit (targetSonar : string) (initialAcousticSettings : AcousticSettings) =
        Log.Information("New sonar conduit {targetSonar}; initial={initialAcousticSettings}",
            targetSonar, initialAcousticSettings)

    let logCloseSonarConduit (targetSonar : string) =
        Log.Information("Close sonar conduit {targetSonar}", targetSonar)

    
    let logSentAcousticSettings (settings : AcousticSettingsVersioned) =
        Log.Information("Sent acoustic settings: {settings}", settings)

    let private _loaded =   Log.logLoad "SonarConduit"

                            let bits32 = "32-bit"
                            let bits64 = "64-bit"
                            let processIntSize = if Environment.Is64BitProcess         then bits64 else bits32
                            let osIntsize = if Environment.Is64BitOperatingSystem then bits64 else bits32
                            Trace.TraceInformation(sprintf "%s process; %s OS; %d processors"
                                                           processIntSize osIntsize Environment.ProcessorCount)

                            try
                                let ifcs = NetworkInterface.GetAllNetworkInterfaces()
                                Trace.TraceInformation("Network interfaces:")
                                let ethernets =
                                    ifcs |> Seq.filter(fun ifc ->
                                                        ifc.NetworkInterfaceType = NetworkInterfaceType.Ethernet)

                                let separator = "--------------------------------------------------"
                                for ifc in ethernets do
                                    Trace.TraceInformation(separator)
                                    Trace.TraceInformation(sprintf "%s %s" ifc.Id ifc.Name)
                                    Trace.TraceInformation(sprintf "    %s" ifc.Description)
                                    Trace.TraceInformation(
                                        sprintf "    status=%A; speed=%d bits/s" ifc.OperationalStatus ifc.Speed)

                                Trace.TraceInformation(separator)
                            with
                                | ex -> Trace.TraceWarning("Could not enumerate network interfaces: " + ex.Message)

    let listenForBeacon (available: AvailableSonars) matchBeacon beaconTarget onFound =

        let disposed = ref false
        let guardObject = Object()
        let listener: IDisposable option ref = ref None

        let cleanUp () =
            lock guardObject (fun () -> match !listener with
                                        | Some ref ->   if not !disposed then
                                                            disposed := true
                                                            ref.Dispose()
                                        | None -> ())

        Trace.TraceInformation(sprintf "listenForBeacon: watching for beacon for '%s'" beaconTarget)
        listener :=
            Some (available.Beacons.Subscribe(fun beacon ->
                        if matchBeacon beacon then
                            Trace.TraceInformation("listenForBeacon: matched beacon for sonar {0} at {1}", beacon.SerialNumber, beacon.SrcIpAddr)
                            onFound beacon
                            cleanUp()))
        { new IDisposable with
            member __.Dispose() = cleanUp() }

    // Focus requests require the environmental inputs needed to calculate sound speed in water as well as system type and
    // whether using a telephoto lens. We don't have these until we've received a frame from the sonar.
    let mkFocusQueue (send: float<m> -> unit) =

        let focusRequestSink = new Subject<float<m>>()
        let focusInputSubscription =
            focusRequestSink.Subscribe(fun range ->
                Log.Information("Sending focus range request for {range}", range)
                send range)

        focusRequestSink, focusInputSubscription
