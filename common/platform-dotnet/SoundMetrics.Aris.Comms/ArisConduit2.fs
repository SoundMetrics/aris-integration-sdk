// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Experimental

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open SoundMetrics.Aris.AcousticSettings
open SoundMetrics.Aris.Comms.Internal
open SoundMetrics.Common
open System
open System.Net
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open SonarConnectionDetails
open SonarConnectionMachineState

open ArisCommands
open ArisConduitDetails
open SoundMetrics.Aris.Comms


type RequestedSettings =
    | SettingsApplied of versioned : AcousticSettingsRaw * constrained : bool
    | SettingsDeclined of string

type FocusPolicy = AutoFocusAtMidfield | ManualFocus


type ArisConduit private (synchronizationContext : SynchronizationContext,
                          initialAcousticSettings : AcousticSettingsRaw,
                          targetSonar : string,
                          matchBeacon : ArisBeacon -> bool,
                          focusPolicy : FocusPolicy,
                          frameStreamReliabilityPolicy : FrameStreamReliabilityPolicy,
                          _perfSink : ConduitPerfSink) as self =

    [<Literal>]
    let LogPrefix = "ArisConduit"

    let _earlyCtor =
        Log.Information(
            LogPrefix + "[{targetSonar}] Constructing ArisConduit; initial settings={initialSettings}",
            targetSonar,
            initialAcousticSettings.ToShortString())
        match SettingsHelpers.validateSettings initialAcousticSettings with
        | ValidationError msg -> invalidArg "initialAcousticSettings" msg
        | _ -> ()

    let disposed = ref false
    let disposingSignal = new ManualResetEventSlim()
    let available = BeaconListener.CreateForArisExplorerAndVoyager(
                        synchronizationContext, TimeSpan.FromSeconds(15.0))
    let mutable serialNumber: Nullable<ArisSerialNumber> = Nullable<ArisSerialNumber>()
    let mutable snListener: IDisposable = null
    let mutable setSalinity: (RawFrame -> unit) = fun _ -> ()
    let mutable environment = ArisEnvironment.Default
    let mutable focusPolicy = focusPolicy
    let systemType: ArisSystemType option ref = ref None
    let cts = new CancellationTokenSource()
    let cxnMgrDone = new ManualResetEventSlim()
    let earlyFrameSubject = new Subject<ArisFinishedFrame>() // Spur from early in the graph
    let frameIndexMapper = new FrameIndexMapper()

    let frameStreamListener =
        new FrameStreamListener(IPAddress.Any, frameStreamReliabilityPolicy)

    let lastRequestedAcoustingSettings = ref AcousticSettingsVersioned.Invalid
    let requestedAcousticSettingsResult = ref Uninitialized
    let currentAcousticSettings = ref AcousticSettingsVersioned.Invalid
    let cookieTracker = SettingsHelpers.AcousticSettingsCookieTracker()
    let acousticSettingsRequestGuard = Object()
    let instantaneousFrameRate: float option ref = ref None
    let cxnStateSubject = new Subject<ConnectionState>()

    let keepAliveTimer = Observable.Interval(SonarConnectionDetails.keepAlivePingInterval)
    let environmentWatcherSub =
        earlyFrameSubject.Subscribe(fun frame -> environment <- frame.Environment)

    // Processing graph
    let graph = ArisGraph.build
                    targetSonar
                    frameStreamListener.Frames
                    earlyFrameSubject

    let buildFrameStreamSubscription () =
        let rateTracker = RateTracker()
        let trackFrameInfo (frame: RawFrame) =
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

        let onError (ex: Exception) =
            Log.Error(
                LogPrefix + "[{targetSonar}] SonarConnection frame error: {exMsg}",
                targetSonar,
                ex.Message)
        frameStreamListener.Frames.Subscribe (trackFrameInfo, onError)

    let frameStreamSubscription = buildFrameStreamSubscription()

    let cxnEvQueue, queueLinks =
        buildCxnEventQueue targetSonar (self :> ISonarConnectionCallbacks)
                           cxnMgrDone
    let inputGraphLinks = wireUpInputEvents cxnEvQueue keepAliveTimer available matchBeacon

    let quitCxnMgr () = cxnEvQueue.Post(mkEventNoCallback CxnEventType.Quit) |> ignore
    let queueCmd (cmd : Aris.Command) =
        if Log.IsEnabled(Serilog.Events.LogEventLevel.Verbose) then
            let commandType = cmd.Type.ToString()
            Log.Verbose(
                LogPrefix + "[{targetSonar}] Queuing command type {commandType}",
                targetSonar,
                commandType)

        cxnEvQueue.Post(mkEventNoCallback (CxnEventType.Command cmd)) |> ignore

    let focusSamplingSubject = new Subject<float<m>>()
    let focusSamplingSubscription =
        let samplePeriod = TimeSpan.FromSeconds(0.3)
        focusSamplingSubject
            .Sample(samplePeriod)
            .DistinctUntilChanged()
            .Subscribe(fun range ->
                let range' = float32 range * 1.0f<m>
                queueCmd (makeFocusCmd range')
                )

    let queueFocusRange range = focusSamplingSubject.OnNext(range)

    let queueAcousticSettings (sv: AcousticSettingsVersioned) : unit =
        queueCmd (makeAcousticSettingsCmd sv)
        logSentAcousticSettings sv

        match focusPolicy with
        | ManualFocus -> ()
        | AutoFocusAtMidfield ->
            let midfieldRange =
                let window = 
                    let env = environment
                    let settings = sv.Settings
                    AcousticMath.CalculateWindow(
                        settings.SampleStartDelay,
                        settings.SamplePeriod,
                        settings.SampleCount,
                        environment.Temperature,
                        environment.Depth,
                        float environment.Salinity)
                window.MidPoint

            queueFocusRange midfieldRange

    let requestAcousticSettings (settings: AcousticSettingsRaw): RequestedSettings =

        Log.Information(
            LogPrefix + "[{targetSonar}] requesting acoustic settings {settings}",
            targetSonar,
            settings.ToShortString())

        match SettingsHelpers.validateSettings settings with
        | ValidationError msg ->
            Log.Error(LogPrefix + "[{targetSonar}] invalid settings: {msg}", targetSonar, msg)
            SettingsDeclined ("Validation error: " + msg)
        | Valid settings ->
            lock acousticSettingsRequestGuard (fun () ->
                let antiAliasing = 0<Us> // REVIEW

                // We need to know the system type in order to constrain settings, so until then we
                // don't constrain. System type is gleaned in ArisConduit.buildFrameStreamSubscription.
                let struct (constrainedSettings, constrained) =
                    match !systemType with
                    | Some systemType ->
                        AcousticMath.ConstrainAcousticSettings(
                            systemType,
                            settings,
                            antiAliasing)
                    | None -> struct (settings, false)

                let versionedSettings =
                    let settings' =
                        if constrained then constrainedSettings else settings
                    cookieTracker.ApplyNewCookie settings'

                lastRequestedAcoustingSettings := versionedSettings
                queueAcousticSettings versionedSettings
                SettingsApplied (versionedSettings.Settings, constrained))

    do
        // Start the listener only after fully initialized--because it modifies this instance.
        snListener <- listenForBeacon available
                                      matchBeacon
                                      targetSonar
                                      (fun beacon ->
                                        self.SerialNumber <- Nullable<ArisSerialNumber>(beacon.SerialNumber))

        logNewArisConduit targetSonar initialAcousticSettings

    /// Find the sonar by its serial number.
    new(
        synchronizationContext,
        initialAcousticSettings,
        sn,
        focusPolicy,
        frameStreamReliabilityPolicy) =
            new ArisConduit(
                synchronizationContext,
                initialAcousticSettings,
                sn.ToString(),
                (fun beacon -> beacon.SerialNumber = sn),
                focusPolicy,
                frameStreamReliabilityPolicy,
                ConduitPerfSink.None)

    /// Find the sonar by its IP address
    new(
        synchronizationContext,
        initialAcousticSettings,
        ipAddress,
        focusPolicy,
        frameStreamReliabilityPolicy) =
            new ArisConduit(
                synchronizationContext,
                initialAcousticSettings,
                ipAddress.ToString(),
                (fun beacon -> beacon.IPAddress = ipAddress),
                focusPolicy,
                frameStreamReliabilityPolicy,
                ConduitPerfSink.None)

    internal new(
                synchronizationContext,
                initialAcousticSettings,
                sn,
                focusPolicy,
                frameStreamReliabilityPolicy,
                perfSink) =
            new ArisConduit(
                synchronizationContext,
                initialAcousticSettings,
                sn.ToString(),
                (fun beacon -> beacon.SerialNumber = sn),
                focusPolicy,
                frameStreamReliabilityPolicy,
                perfSink)

    interface IDisposable with
        member __.Dispose() =
            disposingSignal.Set()
            let disposables =
                List.concat [
                    queueLinks
                    inputGraphLinks
                    [ environmentWatcherSub
                      focusSamplingSubscription
                      focusSamplingSubject
                      earlyFrameSubject
                      frameStreamSubscription
                      frameStreamListener
                      snListener
                      cxnStateSubject
                      cts
                      cxnMgrDone ] ]
            Dispose.theseWith disposed
                disposables
                (fun () ->
                        focusSamplingSubject.OnCompleted()
                        earlyFrameSubject.OnCompleted()
                        graph.CompleteAndWait(TimeSpan.FromSeconds(2.0)) |> ignore
                        graph.Dispose()

                        quitCxnMgr()
                        cts.Cancel()
                        //attemptedAcousticSettingsSubject.OnCompleted()
                        cxnEvQueue.Complete()
                        cxnEvQueue.Completion.Wait()
                        cxnMgrDone.Wait())
            logCloseArisConduit(targetSonar)

    member s.Dispose() = (s :> IDisposable).Dispose()

    interface ISonarConnectionCallbacks with
        member __.OnCxnStateChanged cxnState =
            Log.Information(
                LogPrefix + "[{targetSonar}] connection state changed to {state}",
                targetSonar,
                cxnState)
            match cxnState with
            | ConnectionState.Connected _ -> ()
            | _ -> frameStreamListener.Flush() // Resets frame index tracker

            cxnStateSubject.OnNext(cxnState)

        member __.OnInitializeConnection frameSinkAddress =
            Log.Information(LogPrefix + "[{targetSonar}] initializing connection", targetSonar)
            let setTimeCmd = makeSetDatetimeCmd DateTimeOffset.Now
            Log.Information(
                LogPrefix + "[{targetSonar}] Setting sonar clock to {dateTime}",
                targetSonar,
                setTimeCmd.DateTime.DateTime)
            queueCmd setTimeCmd |> ignore
            let sink = frameStreamListener.SetSinkAddress frameSinkAddress
            queueCmd (makeFramestreamReceiverCmd sink) |> ignore

            let settings =
                let requested = !lastRequestedAcoustingSettings
                let hasExistingSettings = not (requested.Cookie = AcousticSettingsVersioned.InvalidAcousticSettingsCookie)
                if hasExistingSettings then
                    Log.Information(
                        LogPrefix + "[{targetSonar}] sending existing settings: {setings}",
                        targetSonar,
                        requested.Settings.ToString())
                    requested.Settings
                else
                    Log.Information(
                        LogPrefix + "[{targetSonar}] sending initial settings: {settings}",
                        targetSonar,
                        initialAcousticSettings.ToString())
                    initialAcousticSettings
            let requestResult = requestAcousticSettings settings
            match requestResult with
            | SettingsDeclined msg ->
                assert false // unexpected & quite bad to have invalid initial settings
                invalidArg "initialSettings" ("bad initial acoustic settings: " + msg)
            | SettingsApplied _ -> true

    member __.SerialNumber
        with get () = serialNumber
        and private set (sn : Nullable<ArisSerialNumber>) = serialNumber <- sn

    member __.ConnectionState with get () = cxnStateSubject :> ISubject<ConnectionState>

    member ac.WaitForConnectionAsync (timeout : TimeSpan) : Task<bool> =

        let doneSignal = new ManualResetEventSlim(false)
        let mutable reachedState = false

        let sub = ac.ConnectionState.Subscribe(fun state ->
                        match state with
                        | ConnectionState.Connected (_ip, _cmdLink) ->
                            reachedState <- true
                            doneSignal.Set() |> ignore
                        | _ -> ())

        Async.StartAsTask(async {
            try
                let disposingHandle = disposingSignal.WaitHandle // Keep a copy in case of dispose race condition
                let waitHandles = [| doneSignal.WaitHandle; disposingHandle |]

                match WaitHandle.WaitAny(waitHandles, timeout) with
                | WaitHandle.WaitTimeout -> ()
                | idx when waitHandles.[idx] = disposingHandle ->
                    raise (ObjectDisposedException "ArisConduit was disposed while waiting for a state change")
                | _ -> ()
            finally
                sub.Dispose()

            return reachedState
        })

    member __.GetMetrics () = makeMetrics instantaneousFrameRate.Value frameStreamListener.Metrics

    // Settings

    /// Send salinity to the sonar; it may take a moment to appear in the frame header.
    member __.SetSalinity (salinity : Salinity) =
        queueCmd (makeSalinityCmd salinity) |> ignore
        setSalinity <- fun frame -> if enum (int frame.Header.Salinity) <> salinity then
                                        queueCmd (makeSalinityCmd salinity) |> ignore
                                    else
                                        setSalinity <- fun _ -> ()

    member __.FocusPolicy
        with get () = focusPolicy
        and set policy = focusPolicy <- policy

    /// Requests the supplied settings; returns a copy with the cookie attached to the request.
    member __.RequestAcousticSettings settings : RequestedSettings = requestAcousticSettings settings

    member __.RequestFocusDistance (range: float<m>) =

        if range < 0.0<m> then
            invalidArg "range" "Range must be greater than zero"
        queueFocusRange range

    // Frames

    /// Observerable stream of frames from the ARIS.
    member __.Frames = earlyFrameSubject :> IObservable<ArisFinishedFrame>

    // Recording

    member __.StartRecording request = graph.Post (GraphCommand (Experimental.StartRecording request))

    member __.StopRecording request =
        frameIndexMapper.RemoveMappingFor request
        graph.Post (GraphCommand (Experimental.StopRecording request))

    /// Facilitates mapping the display frame index to the recorded frame index.
    /// Returns None if the mapping cannot be done. Mapping can be done only during
    /// recording.
    member internal __.FrameIndexMapper = frameIndexMapper

    // Rotator

    member __.SetRotatorMount mountType = queueCmd (makeSetRotatorMountCmd mountType)
    member __.SetRotatorVelocity (axis: RotatorAxis) velocity = queueCmd (makeSetRotatorVelocityCmd axis velocity)
    member __.SetRotatorAcceleration (axis: RotatorAxis) acceleration = queueCmd (makeSetRotatorAccelerationCmd axis acceleration)
    member __.SetRotatorPosition (axis: RotatorAxis) position = queueCmd (makeSetRotatorPositionCmd axis position)
    member __.StopRotator (axis: RotatorAxis) = queueCmd (makeStopRotatorCmd axis)
