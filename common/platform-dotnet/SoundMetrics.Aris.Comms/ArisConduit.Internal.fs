// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.AcousticSettings
open SoundMetrics.Aris.Comms
open SoundMetrics.Common
open System
open System.Net
open System.Threading.Tasks

module internal ArisConduitHelpers =

    type ParsedTargetId =
    | SN of sn: ArisSerialNumber
    | IP of ip: IPAddress
    | Unknown of msg: string

    /// Contains the context required to correctly set focus. Each of the fields affects focus.
    /// You should generally get this context from a frame received from the sonar.
    type FocusContext = {
        SystemType: ArisSystemType
        Temperature: float<degC>
        Depth: float<m>
        Salinity: Salinity
        Telephoto: bool
        ObservedFocus: FU }
    with
        static member FromFrame (fr : RawFrame) =
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
    let targetIdFromStringAsync (s : string) : Task<ParsedTargetId> =
        try
            let parsed, sn = UInt32.TryParse(s)
            if parsed then
                Task.FromResult (SN sn)
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
                        | ex -> Log.Error("Exception: couldn't resolve '{target}' {exMsg} {stackTrace}", s, ex.Message, ex.StackTrace)
                                Unknown (sprintf "Error: couldn't resolve '%s' %s" s ex.Message)
                        )
        with
            | ex -> Task.FromResult (Unknown (sprintf "Error: %s" ex.Message))


module internal ArisConduitDetails =
    open System.Net.NetworkInformation
    open SoundMetrics.Common

    let logNewArisConduit (targetSonar : string) (initialAcousticSettings : AcousticSettingsRaw) =
        Log.Information("New ARIS conduit {targetSonar}; initial={initialAcousticSettings}",
            targetSonar, initialAcousticSettings)

    let logCloseArisConduit (targetSonar : string) =
        Log.Information("Close ARIS conduit {targetSonar}", targetSonar)


    let logSentAcousticSettings (settings : AcousticSettingsVersioned) =
        Log.Information("Sent acoustic settings: {settings}", settings)

    let private _loaded =   Log.logLoad "ArisConduit"

                            let bits32 = "32-bit"
                            let bits64 = "64-bit"
                            let processIntSize = if Environment.Is64BitProcess    then bits64 else bits32
                            let osIntsize = if Environment.Is64BitOperatingSystem then bits64 else bits32

                            Log.Information("{processIntSize} process; {osIntSize} OS; {processorCount} processors",
                                            processIntSize, osIntsize, Environment.ProcessorCount)

                            try
                                let ifcs = NetworkInterface.GetAllNetworkInterfaces()
                                Log.Information("Network interfaces:")
                                let ethernets =
                                    ifcs |> Seq.filter(fun ifc ->
                                                        ifc.NetworkInterfaceType = NetworkInterfaceType.Ethernet)

                                let separator = "--------------------------------------------------"
                                for ifc in ethernets do
                                    Log.Information(separator)
                                    Log.Information("{ifcId} {ifcName}", ifc.Id, ifc.Name)
                                    Log.Information("    {ifcDesc}", ifc.Description)
                                    Log.Information("    status={status}; speed={speed} bits/s", ifc.OperationalStatus, ifc.Speed)

                                Log.Information(separator)
                            with
                                | ex -> Log.Warning("Could not enumerate network interfaces: {exMsg}", ex.Message)

    let listenForBeacon (available: BeaconListener) matchBeacon (beaconTarget : string) onFound =

        let disposed = ref false
        let guardObject = Object()
        let listener: IDisposable option ref = ref None

        let cleanUp () =
            lock guardObject (fun () -> match !listener with
                                        | Some ref ->   if not !disposed then
                                                            disposed := true
                                                            ref.Dispose()
                                        | None -> ())

        Log.Information("listenForBeacon: watching for beacon for '{target}'", beaconTarget)
        listener :=
            Some (available.AllBeacons.Subscribe(fun device ->
                        match device with
                        | Aris beacon ->
                            if matchBeacon beacon then
                                Log.Information("listenForBeacon: matched beacon for sonar {sn} at {ipAddr}", beacon.SerialNumber, beacon.IPAddress)
                                onFound beacon
                                cleanUp()
                        | _ -> () )
            )
        { new IDisposable with
            member __.Dispose() = cleanUp() }
