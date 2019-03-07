namespace SoundMetrics.Network

open System
open System.Diagnostics
open System.Net
open System.Net.NetworkInformation
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks

module DeviceReachability =
    type ReachabilityStatus = Nominal | NotReachable

    module internal Details =
        open System.Net.NetworkInformation

        let pingTimeout = 1000
        let pingInterval = TimeSpan.FromSeconds(4.0)

        // The reachability timeout is set long enough to ensure we don't get to 'unreachable' before
        // ARIScope removes a newly missing sonar from the display (modulo latency & jitter).
        // The actual timeout time will be extended by PingTimeout's value.
        let reachabilityTimeout = TimeSpan.FromSeconds(6.0)

        type NominalSubstate = Okay | TimedOut of DateTime

        type State =
            | Nominal of NominalSubstate
            | NotReachable
        with
            static member Start = Nominal Okay

        type FsmOutputLevel = Debug | Inform | Warning | Error

        type HandleEventFn = DateTime -> State -> IPStatus -> State

        let matchSuccess(status: IPStatus) = status = IPStatus.Success;
        let matchTimedOut(status: IPStatus) = status = IPStatus.TimedOut;
        let matchNotSuccess(status: IPStatus) = status <> IPStatus.Success;
        let matchNotSuccessNorTimedOut(status: IPStatus) =
            status <> IPStatus.Success && status <> IPStatus.TimedOut;

        let handleEvent
                (timeout: TimeSpan)
                (now: DateTime)
                (currentState: State)
                (ev: IPStatus)
                : State =

            let nextState =
                if ev = IPStatus.Success then
                    Nominal Okay
                else
                    // Failing & failure cases
                    match currentState with
                    | Nominal substate ->
                        match substate with
                        | Okay -> Nominal (TimedOut now)
                        | TimedOut startOfTimeout ->
                            let isTimedOut = timeout < (now - startOfTimeout)
                            if isTimedOut then
                                NotReachable
                            else
                                currentState
                    | NotReachable -> currentState

            nextState

        let createEventHandler(terminateTimeout: TimeSpan) : HandleEventFn =

            fun now currentState ev ->
                handleEvent terminateTimeout now currentState ev


open DeviceReachability

/// <summary>
/// Keeps tabs on whether a network device is reachable. E.g., an ARIS on a 192.168
/// subnet still appears on a 169.254 subnet via broadcast availability messages,
/// though it cannot be commanded from the other subnet. The most reliable way to
/// determine if a network device is reachable is to ping it.
/// 
/// One instance per network device is required.
/// </summary>
[<Sealed>]
type DeviceReachability (getAddress: Func<IPAddress>, postToDispatcher: Action<Action>) as self =
    inherit fracas.NotifyBase()

    let mutable terminate = 0
    let mutable disposed = false
    let mutable state = Details.State.Start
    let handleEventFSM = Details.createEventHandler Details.reachabilityTimeout

    let mutable reachability =
        self |> fracas.mkField <@ self.Reachability @> ReachabilityStatus.Nominal

    let reachabilitySubject = new Subject<_>()
    let ping = new Ping()

    let terminateActivity () = Interlocked.Exchange(ref terminate, 1) |> ignore

    let dispose () =

        if not disposed then
            disposed <- true
            reachabilitySubject.OnCompleted()
            reachabilitySubject.Dispose()
            terminateActivity()
            ping.Dispose()
            GC.SuppressFinalize(self)

    let rec sendPing () =

        if terminate = 0 then
            try
                let targetAddress = getAddress.Invoke()

                ping.SendPingAsync(targetAddress, Details.pingTimeout)
                    .ContinueWith(handlePingResult) |> ignore
            with
            | :? ObjectDisposedException -> () // Race condition between asynchronous task and dispose.
            | ex ->
                Trace.TraceWarning(sprintf "Unexpected exception in DeviceReachability.sendPing: '%s" ex.Message)
            ()
        else
            () // Don't do anything more.


    and handlePingResult (reply: Task<PingReply>) =

        if terminate = 0 then
            state <- handleEventFSM DateTime.Now state reply.Result.Status
            let newReachability =
                match state with
                | Details.NotReachable -> ReachabilityStatus.NotReachable
                | _ -> ReachabilityStatus.Nominal

            let updateReachability = Action(fun () -> self.Reachability <- newReachability)
            postToDispatcher.Invoke(updateReachability)

            Task.Delay(Details.pingInterval).ContinueWith(fun _ -> sendPing()) |> ignore
        else
            () // Don't do anything more.

    do
        if isNull getAddress then
            invalidArg "getAddress" "may not be null"
        if isNull postToDispatcher then
            invalidArg "postToDispatcher" "may not be null"

        sendPing()

    interface IDisposable with
        member __.Dispose() = dispose()
    member __.Dispose() = (self :> IDisposable).Dispose()

    static member PingTimeout = Details.pingTimeout
    static member PingInterval = Details.pingInterval

    member __.Reachability
        with get() = reachability.Value
        and private set newValue =
            if reachability.Set(newValue) then
                try
                    reachabilitySubject.OnNext(newValue)
                with
                :? ObjectDisposedException -> () // a race condition exists with disposal.

    member __.ReachabilityObservable = reachabilitySubject :> IObservable<ReachabilityStatus>
