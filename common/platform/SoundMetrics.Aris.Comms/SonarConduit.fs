// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open System
open System.Net
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open SonarConnectionDetails
open SonarConnectionMachineState
open SoundMetrics.Aris.Config

module SonarConduitHelpers =

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
            let parsed, sn = Int32.TryParse(s)
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
                        | ex -> let msg = sprintf "Exception: couldn't resolve '%s' %s %s" s ex.Message ex.StackTrace
                                System.Diagnostics.Trace.TraceError(msg)
                                System.Diagnostics.Debug.WriteLine(msg)
                                Unknown (sprintf "Error: couldn't resolve '%s' %s" s ex.Message)
                        )
        with
            | ex -> Task.FromResult (Unknown (sprintf "Error: %s" ex.Message))


module private SonarConduitDetails =
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

    type FocusInput =
    | Range of float<m>
    | FocusEnvironment of SonarConduitHelpers.FocusContext

    type FocusRequested = {
        Mapped : FocusMap.MappedFocusUnits
        FocusEnvironment : SonarConduitHelpers.FocusContext
    }

    // Focus requests require the environmental inputs needed to calculate sound speed in water as well as system type and
    // whether using a telephoto lens. We don't have these until we've received a frame from the sonar.
    let mkFocusQueue (frames: ISubject<ProcessedFrame>)
                     (send: float<m> -> FU -> unit) =

        let focusRequestSink = new Subject<float<m>>()
        let pendingFocusRequest: float<m> option ref = ref None
        let environment: SonarConduitHelpers.FocusContext option ref = ref None
        let focusInputSubscription =
            Observable.Merge<FocusInput>([ focusRequestSink.Select(fun range -> Range range)
                                           frames.Where(fun pf -> match pf.work with | Frame (_frame, _histo, _isRecording) -> true | _ -> false)
                                                 .Select(fun f -> match f.work with
                                                                  | Frame (fr, _histo, _isRecording) ->
                                                                        FocusEnvironment (SonarConduitHelpers.FocusContext.FromFrame fr)
                                                                  | _ -> failwith "logic error: should receive only frames") ])
                      .Subscribe(fun input ->

                            let sendIfContextDiffers range (env: SonarConduitHelpers.FocusContext) =
                                let requestedFocus =
                                    (FocusMap.mapRangeToFocusUnits env.SystemType range env.Temperature env.Salinity env.Telephoto).FocusUnits

                                let minFocusDeltaAllowedToSend = 4
                                let diff = abs ((int env.ObservedFocus) - (int requestedFocus))
                                if diff >= minFocusDeltaAllowedToSend then
                                    send range (uint32 requestedFocus)

                            // Handle both Range and FocusEnvironment inputs.
                            match input, !environment, !pendingFocusRequest with
                            | Range r, Some env, _ ->   sendIfContextDiffers r env
                                                        pendingFocusRequest := None

                            | Range r, None,     _ ->   pendingFocusRequest := Some r

                            | FocusEnvironment fe, _, Some r -> sendIfContextDiffers r fe
                                                                pendingFocusRequest := None
                                                                environment := Some fe

                            | FocusEnvironment fe, _, None ->   environment := Some fe
                         )

        focusRequestSink, focusInputSubscription

open ArisCommands
open SonarConduitDetails
open System.Diagnostics

type RequestedSettings =
    | SettingsApplied of versioned: AcousticSettingsVersioned * constrained: bool
    | SettingsDeclined of string


type SonarConduit private (conduitOptions : ConduitOptions,
                           initialAcousticSettings: AcousticSettings,
                           available: AvailableSonars,
                           targetSonar: string,
                           matchBeacon: SonarBeacon -> bool,
                           frameStreamReliabilityPolicy: FrameStreamReliabilityPolicy) as self =

    let disposed = ref false
    let mutable serialNumber: SerialNumber option = None
    let mutable snListener: IDisposable = null
    let mutable setSalinity: (Frame -> unit) = fun _ -> ()
    let systemType: SystemType option ref = ref None
    let cts = new CancellationTokenSource()
    let cxnMgrDone = new ManualResetEventSlim()
    let earlyFrameSubject = new Subject<ProcessedFrame>() // Spur from early in the graph
    let frameIndexMapper = new FrameIndexMapper()

    // Lie about the frame sink IP address until we get a connection
    let frameStreamListener =
        new FrameStreamListener(IPAddress.Any, frameStreamReliabilityPolicy)

    let lastRequestedAcoustingSettings = ref AcousticSettingsConstants.invalidAcousticSettingsVersioned
    let requestedAcousticSettingsResult = ref Uninitialized
    let currentAcousticSettings = ref AcousticSettingsConstants.invalidAcousticSettingsVersioned
    //let attemptedAcousticSettingsSubject = new Subject<AcousticSettingsApplied>()
    let cookieTracker = SettingsHelpers.AcousticSettingsCookieTracker()
    let acousticSettingsRequestGuard = Object()
    let instantaneousFrameRate: float option ref = ref None
    let cxnStateSubject = new Subject<CxnState>()

    let keepAliveTimer = Observable.Interval(SonarConnectionDetails.keepAlivePingInterval)

    // Processing graph
    let pGraph, pGraphLeaves, pGraphDisposables =
        GraphBuilder.buildSimpleRecordingGraph frameStreamListener.Frames
                                               conduitOptions
                                               earlyFrameSubject
                                               frameIndexMapper.ReportFrameMapping

    let buildFrameStreamSubscription () =
        let rateTracker = RateTracker()
        let trackFrameInfo (frame: Frame) =
            match !systemType with
            | Some st -> if st <> (enum (int frame.Header.TheSystemType)) then
                            systemType := Some (enum (int frame.Header.TheSystemType))
            | None ->    systemType := Some (enum (int frame.Header.TheSystemType))

            match rateTracker.MarkNewFrame frame.Header.FrameTime with
            | Some us -> instantaneousFrameRate := Some(1000000.0 / float us)
            | None -> ()

            lock acousticSettingsRequestGuard (fun () ->
                let settings = frame.GetAcousticSettings()
                requestedAcousticSettingsResult := settings.AppliedSettings
                currentAcousticSettings := settings.CurrentSettings)

            setSalinity frame

        let onError (ex: Exception) = printfn "SonarConnection frame error: %s" ex.Message
        let onCompleted () = () // TODO?

        frameStreamListener.Frames.Subscribe (trackFrameInfo, onError, onCompleted)

    let frameStreamSubscription = buildFrameStreamSubscription()

    let cxnEvQueue, queueLinks =
        buildCxnEventQueue targetSonar (self :> ISonarConnectionCallbacks)
                           cxnMgrDone
    let inputGraphLinks = wireUpInputEvents cxnEvQueue keepAliveTimer available matchBeacon

    let quitCxnMgr () = cxnEvQueue.Post(mkEventNoCallback Quit) |> ignore
    let queueCmd (cmd : Aris.Command) =
        if Serilog.Log.IsEnabled(Serilog.Events.LogEventLevel.Verbose) then
            let commandType = cmd.Type.ToString()
            Serilog.Log.Verbose("Queuing command type {commandType}", commandType)

        cxnEvQueue.Post(mkEventNoCallback (Command cmd)) |> ignore
        // TODO mkEventNoCallback determine if we really want callbacks; how to implement in API

    let focusRequestSink, focusInputSubscription =
        mkFocusQueue (earlyFrameSubject :> ISubject<ProcessedFrame>)
                     (fun (range: float<m>) requestedFocus ->
                        Trace.TraceInformation("SonarConduit: Queuing focus request command for range {0}; focus units={1}",
                                               range, requestedFocus)
                        let cmd = makeFocusCmd requestedFocus
                        queueCmd cmd)

    let requestAcousticSettings (settings: AcousticSettings): RequestedSettings =

        Trace.TraceInformation("SonarConduit({0}): requesting acoustic settings {1}", targetSonar, settings.ToShortString())

        match SettingsHelpers.validateSettings settings with
        | ValidationError msg -> Trace.TraceError("SonarConduit({0}): invalid settings: {1}", targetSonar, msg)
                                 SettingsDeclined ("Validation error: " + msg)
        | Valid settings ->
            lock acousticSettingsRequestGuard (fun () ->
                let antiAliasing = 0<Us> // REVIEW

                // We need to know the system type in order to constrain settings, so until then we
                // don't constrain. System type is gleaned in SonarConduit.buildFrameStreamSubscription.
                let constrainedSettings, constrained =
                    match !systemType with
                    | Some systemType -> AcousticMath.constrainAcousticSettings systemType settings antiAliasing
                    | None -> settings, false

                let settings = if constrained then constrainedSettings else settings
                let versionedSettings = cookieTracker.ApplyNewCookie settings

                lastRequestedAcoustingSettings := versionedSettings
                queueCmd (makeAcousticSettingsCmd versionedSettings)
                logSentAcousticSettings versionedSettings
                SettingsApplied (versionedSettings, constrained))

    do
        // Start the listener only after fully initialized--because it modifies this instance.
        snListener <- listenForBeacon available matchBeacon targetSonar (fun beacon -> self.SerialNumber <- Some beacon.SerialNumber)
        logNewSonarConduit targetSonar initialAcousticSettings

    /// Find the sonar by its serial number.
    new(initialAcousticSettings, sn, availableSonars, frameStreamReliabilityPolicy) =
            new SonarConduit({ AlternateReordering = None },
                             initialAcousticSettings,
                             availableSonars,
                             sn.ToString(),
                             (fun beacon -> beacon.SerialNumber = sn),
                             frameStreamReliabilityPolicy)

    /// Find the sonar by its IP address
    new(initialAcousticSettings, ipAddress, availableSonars, frameStreamReliabilityPolicy) = 
            new SonarConduit({ AlternateReordering = None },
                             initialAcousticSettings,
                             availableSonars,
                             ipAddress.ToString(),
                             (fun beacon -> beacon.SrcIpAddr = ipAddress),
                             frameStreamReliabilityPolicy)

    /// Internal IP version for testing.
    internal new(conduitOptions, initialAcousticSettings, ipAddress, availableSonars, frameStreamReliabilityPolicy) = 
            new SonarConduit(conduitOptions,
                             initialAcousticSettings,
                             availableSonars,
                             ipAddress.ToString(),
                             (fun beacon -> beacon.SrcIpAddr = ipAddress),
                             frameStreamReliabilityPolicy)

    interface IDisposable with
        member __.Dispose() =
            let disposables = List.concat [ queueLinks; inputGraphLinks; pGraphDisposables
                                            [ focusInputSubscription; earlyFrameSubject; frameStreamSubscription
                                              frameStreamListener; snListener; cxnStateSubject; cts; cxnMgrDone ] ]
            Dispose.theseWith disposed
                disposables
                (fun () ->
                        GraphBuilder.quitWaitClean (pGraph, pGraphLeaves, pGraphDisposables)

                        quitCxnMgr()
                        cts.Cancel()
                        earlyFrameSubject.OnCompleted()
                        //attemptedAcousticSettingsSubject.OnCompleted()
                        cxnEvQueue.Complete()
                        cxnEvQueue.Completion.Wait()
                        cxnMgrDone.Wait())
            logCloseSonarConduit(targetSonar)

    member s.Dispose() = (s :> IDisposable).Dispose()

    interface ISonarConnectionCallbacks with
        member __.OnCxnStateChanged cxnState =
            Trace.TraceInformation("SonarConduit({0}): connection state changed to {1}", targetSonar, cxnState)
            match cxnState with
            | CxnState.Connected _ -> ()
            | _ -> frameStreamListener.Flush() // Resets frame index tracker
            
            cxnStateSubject.OnNext(cxnState)

        member s.OnInitializeConnection frameSinkAddress =
            Trace.TraceInformation("SonarConduit[{0}]: initializing connection", targetSonar)
            let setTimeCmd = makeSetDatetimeCmd DateTimeOffset.Now
            Log.Information("Setting sonar clock to {dateTime}", setTimeCmd.DateTime.DateTime)
            queueCmd setTimeCmd |> ignore
            let sink = frameStreamListener.SetSinkAddress frameSinkAddress
            queueCmd (makeFramestreamReceiverCmd sink) |> ignore

            let settings =
                let requested = s.RequestedAcousticSettings
                let hasExistingSettings = not (requested.Cookie = AcousticSettingsConstants.invalidAcousticSettingsCookie)
                if hasExistingSettings
                    then System.Diagnostics.Trace.TraceInformation ("sending existing settings" + requested.Settings.ToString())
                         requested.Settings
                    else System.Diagnostics.Trace.TraceInformation ("sending intiial settings" + initialAcousticSettings.ToString())
                         initialAcousticSettings
            let requestResult = requestAcousticSettings settings
            match requestResult with
            | SettingsDeclined msg ->
                assert false // unexpected & quite bad to have invalid initial settings
                invalidArg "initialSettings" ("bad initial acoustic settings: " + msg)
            | SettingsApplied _ -> true

    member __.SerialNumber
        with get () = serialNumber
        and private set sn = serialNumber <- sn

    member __.ConnectionState with get () = cxnStateSubject :> ISubject<CxnState>

    member __.Metrics = makeMetrics instantaneousFrameRate.Value frameStreamListener.Metrics

    // Settings

    /// The most-recently requested settings; may not match currently applied settings.
    member __.RequestedAcousticSettings = lastRequestedAcoustingSettings.Value

    member __.RequestedAcousticSettingsResult = requestedAcousticSettingsResult.Value

    /// Send salinity to the sonar; it may take a moment to appear in the frame header.
    member __.SetSalinity (salinity : Salinity) =
        queueCmd (makeSalinityCmd salinity) |> ignore
        setSalinity <- fun frame -> if enum (int frame.Header.Salinity) <> salinity then
                                        queueCmd (makeSalinityCmd salinity) |> ignore
                                    else
                                        setSalinity <- fun _ -> ()

    /// The current acoustic settings as applied by the sonar.
    member __.CurrentAcousticSettings = currentAcousticSettings.Value

    /// Stream of attempted acoustic settings returned from the sonar, applied, constrained, or invalid.
    //member s.AttemptedAcousticSettings = attemptedAcousticSettingsSubject :> ISubject<AcousticSettingsApplied>

    /// Requests the supplied settings; returns a copy with the cookie attached to the request.
    member __.RequestAcousticSettings settings : RequestedSettings = requestAcousticSettings settings

    member __.RequestFocusDistance (range: float<m>) = focusRequestSink.OnNext(range)

    // Frames

    /// Stream of frames from the sonar.
    member __.Frames = earlyFrameSubject :> IObservable<ProcessedFrame>

    // Recording

    member __.StartRecording request = pGraph.Post (WorkUnit.Command (StartRecording request))
    member __.StopRecording request =
        frameIndexMapper.RemoveMappingFor request
        pGraph.Post (WorkUnit.Command (StopRecording request))

    /// Facilitates mapping the display frame index to the recorded frame index.
    /// Returns None if the mapping cannot be done. Mapping can be done only during
    /// recording.
    member __.FrameIndexMapper = frameIndexMapper

    // Rotator

    member __.SetRotatorMount mountType = queueCmd (makeSetRotatorMountCmd mountType)
    member __.SetRotatorVelocity (axis: RotatorAxis) velocity = queueCmd (makeSetRotatorVelocityCmd axis velocity)
    member __.SetRotatorAcceleration (axis: RotatorAxis) acceleration = queueCmd (makeSetRotatorAccelerationCmd axis acceleration)
    member __.SetRotatorPosition (axis: RotatorAxis) position = queueCmd (makeSetRotatorPositionCmd axis position)
    member __.StopRotator (axis: RotatorAxis) = queueCmd (makeStopRotatorCmd axis)
