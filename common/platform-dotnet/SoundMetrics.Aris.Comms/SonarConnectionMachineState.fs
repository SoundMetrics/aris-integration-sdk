// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms.Internal

open Serilog
open SoundMetrics.Aris.Comms
open System
open System.Diagnostics
open System.Net
open System.Net.Sockets
open System.Reactive.Linq
open System.Threading
open System.Threading.Tasks.Dataflow

/// Implementation of SonarConnection's state machine.
module internal SonarConnectionMachineState =

    let logConnectionStateChange (targetSonar : string) (state : string) =
        Log.Information("Connection state of '{targetSonar}' changed to '{state}'", targetSonar, state)

    let logSocketException (msg : string) = Log.Warning("Socket exception: '{msg}'", msg)

    /// Types of events that are queued on the connection; these affect connection state.
    type CxnEventType = | KeepAliveTick
                        | Beacon of SonarBeacon
                        | Command of Aris.Command
                        | LostConnection
                        | Quit

    /// Used to communicate whether the event has successfully queued/handled.
    type CxnEventCallback = bool -> unit

    /// CxnEventType + callback
    type CxnEvent = CxnEventType * CxnEventCallback

    (* Valid CxnState state changes:
        SonarNotFound -> NotConnected
        NotConnected -> Connected
        NotConnected -> ConnectionRefused
        NotConnected -> SonarNotFound
        Connected -> NotConnected
        ConnectionRefused -> Connected
        ConnectionRefused -> ConnectedConnectionRefused
        * -> Closed
    *)

    /// Callbacks used to indicate state changes and allow initialization
    /// on the sonar connection.
    type ISonarConnectionCallbacks =
        abstract member OnCxnStateChanged: ConnectionState -> unit
        abstract member OnInitializeConnection: frameSinkAddress : IPAddress -> bool

    let closeCmdLink (cmdLink: TcpClient) = cmdLink.Close()

    // State transition helpers; these affect the transition and check for
    // bad state transitions

    let describeBadTransition state = sprintf "Bad transition from state [%s]" (state.ToString())

    // Use these functions to affect state changes.

    let foundSonar ip state =
        match state with
        | SonarNotFound -> NotConnected ip
        | NotConnected _ -> NotConnected ip
        | _ -> failwith (describeBadTransition state)

    let lostSonar state =
        match state with
        | NotConnected _ip | ConnectionRefused _ip -> SonarNotFound
        | _ -> failwith (describeBadTransition state)

    let connected cmdLink state =
        match state with
        | NotConnected _ip
        | ConnectionRefused _ip -> Connected cmdLink
        | _ -> failwith (describeBadTransition state)

    let refused ip state =
        match state with
        | NotConnected _
        | ConnectionRefused _ -> ConnectionRefused ip
        | _ -> failwith (describeBadTransition state)

    let disconnected state =
        match state with
        | Connected (ip, cmdLink) ->
            closeCmdLink cmdLink
            NotConnected ip
        | _ -> failwith (describeBadTransition state)

    let close state =
        match state with
        | Connected (_ip, cmdLink) -> closeCmdLink cmdLink
        | _ -> ()
        Closed


    /// State maintained across calls to handleEvent.
    type EventHandlerState = {
        mutable machineState: ConnectionState
        mutable sonarIP: IPAddress
        targetSonar: string
        callbacks: ISonarConnectionCallbacks
        doneSignal: ManualResetEventSlim }
    with
        static member Create targetSonar callbacks doneSignal =
            { machineState = SonarNotFound
              sonarIP = IPAddress(0L)
              targetSonar = targetSonar
              callbacks = callbacks
              doneSignal = doneSignal }
        member s.ChangeState (newState: ConnectionState) =
            s.machineState <- newState
            let stateString = newState.ToString()
            logConnectionStateChange s.targetSonar stateString
            Trace.TraceInformation(sprintf "EventHandlerState[%s]: state changed to [%s]" 
                                    s.targetSonar stateString)

    /// Builds the TCP command link with appropriate settings.
    let buildCmdLink (ip: IPAddress) =
        let client = new TcpClient()
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true)
        let ep = IPEndPoint(ip, NetworkConstants.SonarTcpNOListenPort) // port 56888
        client.Connect(ep)
        client

    /// Handles every type of input event given to the sonar.
    let handleEvent (state: EventHandlerState) (event: CxnEvent) =
        let mutable success = false
        let machineStateBegin = state.machineState
        let ev, callback = event

        try
            match ev with
            | KeepAliveTick ->
                match state.machineState with
                | Connected (_ip, cmdLink) ->
                    SonarConnectionDetails.sendCmd
                        cmdLink ArisCommands.pingCommandSingleton
                    |> ignore
                | _ -> ()

            | Beacon beacon ->
                let connect () =
                    let targetAddr = beacon.SrcIpAddr
                    try
                        let cmdLink = buildCmdLink targetAddr
                        let frameSinkAddr = (cmdLink.Client.LocalEndPoint :?> IPEndPoint).Address
                        if state.callbacks.OnInitializeConnection frameSinkAddr then
                            state.ChangeState (connected (targetAddr, cmdLink) state.machineState)
                        else
                            state.ChangeState (NotConnected targetAddr)
                    with
                    | :? System.Net.Sockets.SocketException as ex ->
                        logSocketException ex.Message
                        Trace.TraceWarning(sprintf "Couldn't connect to %A: %s" targetAddr ex.Message)
                        state.ChangeState (refused targetAddr state.machineState)
                    
                match state.machineState with
                | SonarNotFound ->
                    state.ChangeState (foundSonar (beacon.SrcIpAddr) state.machineState)
                    connect()
                | Connected (_ip, cmdLink) -> assert ((cmdLink.Client.RemoteEndPoint :?> IPEndPoint).Address = beacon.SrcIpAddr)
                | NotConnected ip
                | ConnectionRefused ip ->
                    if ip <> beacon.SrcIpAddr then
                        state.ChangeState (lostSonar state.machineState)
                        state.ChangeState (foundSonar beacon.SrcIpAddr state.machineState)
                    connect()
                | Closed -> ()

            | Command cmd ->
                match state.machineState with
                | Connected (_ip, cmdLink) ->
                    SonarConnectionDetails.sendCmd cmdLink cmd |> ignore
                    success <- true
                | _ -> ()

            | LostConnection ->
                state.ChangeState (disconnected state.machineState)

            | Quit ->
                state.ChangeState (close state.machineState)
                state.doneSignal.Set()
        with
            | :? SocketException ->
                match state.machineState with
                | Connected (_ip, _cmdLink) -> state.ChangeState (disconnected state.machineState)
                | _ -> ()

        if machineStateBegin <> state.machineState then
            try
                state.callbacks.OnCxnStateChanged(state.machineState) |> ignore
            with
            | ex ->
                Trace.TraceError ("OnCxnStateChanged: an exception occurred: {0}", ex.Message)
                Trace.TraceError (ex.StackTrace)

        callback, success

    /// Builds the connection event queue (dataflow graph); returns the input target
    /// and the graph links that later must be disposed.
    let buildCxnEventQueue targetSonar callbacks cxnMgrDone =
        let eventHandler =
            // Curry the event handler state with the event handler--nobody else can see it.
            let evHandlerState = EventHandlerState.Create targetSonar callbacks cxnMgrDone
            handleEvent evHandlerState
        
        let inBuffer = BufferBlock<CxnEvent>()
        let txf = TransformBlock<CxnEvent, CxnEventCallback * bool>(eventHandler)
        let outBuffer = BufferBlock<CxnEventCallback * bool>()
        let notify = ActionBlock<CxnEventCallback * bool>(fun (callback, success) -> callback success)
        let evQueuelinks = [
            inBuffer.LinkTo(txf)
            txf.LinkTo(outBuffer)
            outBuffer.LinkTo(notify)
        ]

        inBuffer, evQueuelinks

    /// Make a connection event with an empty callback.
    let mkEventNoCallback evt = evt, fun _ -> ()

    /// Wires up and subscribes to all the input events; returns a list of subscriptions
    /// that must be disposed on final disposal of the connection.
    let wireUpInputEvents (evQueue: ITargetBlock<CxnEvent>) (keepAliveTimer: IObservable<int64>)
                          (available: AvailableSonars) (matchBeacon: SonarBeacon -> bool) =
        let subscriptions = [
            keepAliveTimer.Subscribe(fun _ -> evQueue.Post(mkEventNoCallback KeepAliveTick) |> ignore)
            available.Beacons
                     .Where(fun beacon -> matchBeacon(beacon))
                     .Subscribe(fun b -> evQueue.Post(mkEventNoCallback (Beacon b)) |> ignore)
        ]
        subscriptions
