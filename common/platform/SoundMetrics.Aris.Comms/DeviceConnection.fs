// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

// Generic device connection management

open System.Net
open System.Net.Sockets

type DeviceCxnState =
    | Start
    | Connected of ep: IPEndPoint // ep for ToString on dead state (cmdLink.Client=null)
    | NotConnected
    | ConnectionRefused of IPEndPoint
    | Closed
with
    override s.ToString() =
        match s with
        | Start -> "start"
        | Connected ep -> sprintf "connected to %s" (ep.ToString())
        | NotConnected -> "not connected"
        | ConnectionRefused ep -> sprintf "connection refused from %s" (ep.ToString())
        | Closed -> "closed"


module internal DeviceConnectionDetails =
    open System
    open System.Diagnostics
    open System.Threading
    open System.Threading.Tasks.Dataflow


    type Work =
    | SetTarget of IPEndPoint
    | SendMessage of byte array

    type ICxnCallbacks =
        abstract member OnStateChange : newState : DeviceCxnState -> unit
        abstract member OnMsgReceived : msg : byte array -> unit


    //-------------------------------------------------------------------------

    type private ReaderInfo = {
        tcp : TcpClient
        reader : FramedMessageReader
        subscription : IDisposable
    }

    let mkThreadProc (callback : ICxnCallbacks)
                     (workSource : ISourceBlock<Work>)
                     doneSignal =

        let errorSignal = new AutoResetEvent(false) // workerFn() takes ownership

        let onMessage msg =

            callback.OnMsgReceived(msg)

        let onError (exn : Exception) =
        
            Trace.TraceWarning(sprintf "Device connection error: %s" exn.Message)
            errorSignal.Set() |> ignore

        let mkConnection (ep : IPEndPoint) =

            let controlTcpKeepAlives (client : Socket) (interval : TimeSpan) (retryInterval : TimeSpan) =

                // Rather than creating a packed struct here we'll just construct the byte array directly.
                let enable = 1u
                let interval = uint32 interval.TotalMilliseconds
                let retryInterval = uint32 retryInterval.TotalMilliseconds

                let values = Array.concat ([ BitConverter.GetBytes(enable)
                                             BitConverter.GetBytes(interval)
                                             BitConverter.GetBytes(retryInterval) ])
                client.IOControl(IOControlCode.KeepAliveValues, values, null) |> ignore

            try
                let tcp = new TcpClient()
                tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true)
                let emptyCallback =
                    let f (_ : IAsyncResult) = ()
                    AsyncCallback(f)

                let ar = tcp.BeginConnect(ep.Address, ep.Port, emptyCallback, null)
                use wh = ar.AsyncWaitHandle

                let connected = wh.WaitOne(TimeSpan.FromSeconds(5.0))

                if connected then
                    tcp.EndConnect(ar)
                    let keepaliveInterval = TimeSpan.FromSeconds(5.0)
                    let keepaliveRetryInterval = TimeSpan.FromSeconds(1.0)
                    controlTcpKeepAlives tcp.Client keepaliveInterval keepaliveRetryInterval

                    try
                        let readerDesc = sprintf "Defender reports for %A" ep
                        let reader = new FramedMessageReader(tcp, 16 * 1024, readerDesc)
                        let subscription = reader.Messages.Subscribe(onMessage, onError)

                        Some { tcp = tcp; reader = reader; subscription = subscription }, Connected ep
                    with
                        ex -> Trace.TraceInformation(sprintf "mkConnection failed : %s" ex.Message)
                              tcp.Close()
                              None, NotConnected
                else
                    tcp.Close()
                    None, NotConnected
            with
            | :? SocketException as ex ->
                Trace.TraceInformation(sprintf "mkConnection socket exception: %s" ex.Message)
                None, (ConnectionRefused ep)
            | _ -> None, NotConnected

        let closeConnection (state : ReaderInfo option) =

            match state with
            | Some state -> state.subscription.Dispose();
                            state.reader.Dispose()
                            state.tcp.Close()
            | None -> ()

            None, Closed

        let workerFn () =

            use errorSignal = errorSignal // take ownership
            use stateChangeSignal = new AutoResetEvent(false)

            let quit = ref false
            let endpoint = ref Option<IPEndPoint>.None
            let readerInfo = ref Option<ReaderInfo>.None
            let state = ref Start
            let stateGuard = obj()

            let setState newState =

                if newState <> !state then
                    state := newState
                    stateChangeSignal.Set() |> ignore
                    callback.OnStateChange(newState)

            use _workProcessor = workSource.LinkTo(ActionBlock<Work>(fun work ->
                if not !quit then
                    lock stateGuard (fun () ->
                        match work with
                        | SetTarget ep ->
                            match !readerInfo with
                            | Some _ ->
                                let setEP =   match !endpoint with
                                              | Some currentEP -> currentEP <> ep
                                              | None -> true
                                if setEP then
                                    let ri, newState = closeConnection !readerInfo
                                    readerInfo := ri
                                    setState newState

                                    endpoint := Some ep

                                    let ri, newState = mkConnection ep
                                    readerInfo := ri
                                    setState newState
                            | None -> endpoint := Some ep

                        | SendMessage msg ->
                            match !readerInfo with
                            | Some ri ->
                                let prefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(msg.Length))
                                ri.tcp.Client.BeginSend([| ArraySegment(prefix); ArraySegment(msg) |],
                                                        SocketFlags.None,
                                                        AsyncCallback ( fun ar ->
                                                            try
                                                                ri.tcp.Client.EndSend(ar) |> ignore
                                                            with
                                                            _ -> () ),
                                                        None) |> ignore
                            | None -> () )
                    ))

            let signals = [| doneSignal; errorSignal; stateChangeSignal |] : WaitHandle array

            let mkConnection ep =
                let ri, newState = mkConnection ep
                readerInfo := ri
                setState newState

            while not !quit do
                let waitPeriod =
                    lock stateGuard (fun () ->
                        match !state with
                        | Start | NotConnected ->
                            assert ((!readerInfo).IsNone)
                            match !endpoint with
                            | Some ep -> mkConnection ep
                            | None -> ()

                        | ConnectionRefused _ep ->
                            assert ((!readerInfo).IsNone)
                            match !endpoint with
                            | Some ep -> mkConnection ep
                            | None -> ()

                        | Connected _ -> ()
                        | Closed -> ()
                    
                        match !state with
                        | Connected _ -> Timeout.InfiniteTimeSpan
                        | _ -> TimeSpan.FromSeconds(2.0) )

                match WaitHandle.WaitAny(signals, waitPeriod) with
                | 0 -> // quit signal
                    lock stateGuard (fun () ->
                        quit := true
                        let ri, newState = closeConnection !readerInfo
                        readerInfo := ri
                        setState newState )

                | 1 -> // error
                    lock stateGuard (fun () ->
                        let ri, newState = closeConnection !readerInfo
                        readerInfo := ri
                        setState newState

                        match !endpoint with
                        | Some ep -> mkConnection ep
                        | None -> () )

                | 2 | WaitHandle.WaitTimeout -> // state change or timeout
                    () // Just wake up to re-examine our situation


                | x -> failwith (sprintf "Unexpected wait result: %d" x)

        workerFn


//-----------------------------------------------------------------------------

open DeviceConnectionDetails
open System
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks.Dataflow

type internal DeviceConnection() =

    let disposed = ref false
    let stateSubject = new Subject<DeviceCxnState>()
    let msgSubject = new Subject<byte array>()
    let workQueue = BufferBlock<Work>()
    let doneSignal = new ManualResetEventSlim()

    let workerThread =
        let callbacks = {
            new ICxnCallbacks with
                member __.OnStateChange newState =
                    System.Diagnostics.Trace.TraceInformation(
                        sprintf "DeviceConnection changed state to %A" newState)
                    stateSubject.OnNext(newState)
                member __.OnMsgReceived msg = msgSubject.OnNext(msg)
        }
        let fn = mkThreadProc callbacks workQueue doneSignal.WaitHandle
        let t = Thread(fn)
        t.Start()
        t

    let queueWork work = workQueue.Post(work)

    interface IDisposable with
        member __.Dispose() =
            Dispose.theseWith disposed
                [ stateSubject; msgSubject; doneSignal ]
                (fun () ->
                    doneSignal.Set()
                    workerThread.Join()
                    msgSubject.OnCompleted()
                    stateSubject.OnCompleted())

    member __.MessagesReceived = msgSubject :> IObservable<byte array>
    member __.State = stateSubject :> IObservable<DeviceCxnState>

    member __.SetTarget ep = queueWork (SetTarget ep)
    member __.SendMessage msg = queueWork (SendMessage msg)
