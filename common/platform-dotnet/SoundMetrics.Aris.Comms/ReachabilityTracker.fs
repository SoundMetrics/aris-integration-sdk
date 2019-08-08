// Copyright (c) 2019 Sound Metrics. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open System
open System.Net
open System.Threading
open System.Reactive.Subjects
open System.Net.NetworkInformation

type ReachabilityStatus = Nominal = 1 | NotReachable = 2

module ReachabilityTrackerFsm =

    type internal FsmOutputLevel = Debug | Inform | Warning | Error

    type ReachabilityNominalSubstate =
        | Okay
        | TimedOut of DateTimeOffset

    type ReachabilityTrackerState =
        | Nominal of ReachabilityNominalSubstate
        | NotReachable

    let stateToStatus = function
        | Nominal _ -> ReachabilityStatus.Nominal
        | NotReachable -> ReachabilityStatus.NotReachable

open ReachabilityTrackerFsm

module internal ReachabilityTrackerDetails =

    let nominalOkay = Nominal Okay

    let handleEvent timeout
                    now
                    currentState
                    (ev : IPStatus)
                    : ReachabilityTrackerState =

        match currentState with
        | Nominal substate ->
            match ev with
            | IPStatus.Success -> nominalOkay
            | IPStatus.TimedOut ->
                match substate with
                | Okay -> Nominal (TimedOut now) // the input 'now' for ease of testing
                | TimedOut timeoutStart ->
                    let exceededTimeout = timeout < now - timeoutStart
                    if exceededTimeout then
                        NotReachable
                    else
                        currentState
            | _ -> NotReachable

        | NotReachable ->
            match ev with
            | IPStatus.Success -> nominalOkay
            | _ -> currentState


    let PingTimeout = 1000
    let PingInterval = TimeSpan.FromSeconds(4.0)

    // The reachability timeout is set long enough to ensure we don't get to 'unreachable' before
    // ARIScope removes a newly missing device from the display (modulo latency & jitter).
    // The actual timeout time will be extended by PingTimeout's value.
    let ReachabilityTimeout = TimeSpan.FromSeconds(6.0)

open ReachabilityTrackerDetails
open System.Diagnostics
open System.Threading.Tasks
open System.Net.Sockets

/// Keeps tabs on whether a network device is reachable. E.g., an ARIS on a 192.168
/// subnet still appears on a 169.254 subnet via broadcast availability messages,
/// though it cannot be commanded from the other subnet. The most reliable way to
/// determine if a network device is reachable is to ping it.
///
/// One instance per network device is required.
type ReachabilityTracker (deviceAddress : IPAddress, syncContext : SynchronizationContext)
        as self =
    inherit fracas.NotifyBase()

    let reachabilityStatus = self |> fracas.mkField <@ self.ReachabilityStatus @> ReachabilityStatus.Nominal

    let reachabilitySubject = new Subject<ReachabilityStatus>()
    let mutable state = nominalOkay
    let mutable terminateFlag = false
    let mutable disposed = false
    let doneSignal = new ManualResetEventSlim()
    let ping = new Ping()


    let invokeOnSyncContext (fn : obj -> unit) : unit =
        let cb = SendOrPostCallback fn
        syncContext.Post (cb, ())

    let rec sendPing () =
        if not terminateFlag then
            try
                ping.SendPingAsync(deviceAddress, PingTimeout)
                    .ContinueWith(handlePingResult) |> ignore
            with
                | :? ObjectDisposedException ->
                    doneSignal.Set()
                    reraise() // Race condition between asynchronous task and dispose.
                | ex ->
                    Trace.TraceWarning("Unexpected exception in ReachabilityTracker.SendPing: {ex.Message}");
                    doneSignal.Set()
        else
            doneSignal.Set() // Don't do anything more.

    and handlePingResult (reply : Task<PingReply>) =
        if not terminateFlag then
            let newState =
                ReachabilityTrackerDetails.handleEvent ReachabilityTimeout
                                                       DateTimeOffset.Now
                                                       state
                                                       reply.Result.Status
            state <- newState
            let newReachabilityStatus = stateToStatus newState
            invokeOnSyncContext(fun _ -> self.ReachabilityStatus <- newReachabilityStatus)

            Task.Delay(PingInterval).ContinueWith(fun _ -> sendPing()) |> ignore
        else
            doneSignal.Set() // Don't do anything more.

    let terminate () = terminateFlag <- true

    let dispose () =
        if not disposed then
            disposed <- true
            reachabilitySubject.OnCompleted()
            reachabilitySubject.Dispose()
            terminate()
            doneSignal.Wait(TimeSpan.FromSeconds(2.0)) |> ignore
            doneSignal.Dispose()
            ping.Dispose()
            GC.SuppressFinalize(self)

    do
        if box syncContext |> isNull then
            raise (ArgumentNullException "syncConext")
        if deviceAddress.AddressFamily <> AddressFamily.InterNetwork then
            invalidArg "deviceAddress" "only IPv4 is supported"

        sendPing()

    interface IDisposable with
        member __.Dispose() = dispose()

    member __.ReachabilityStatus
        with get () : ReachabilityStatus = reachabilityStatus.Value
        and  set (newValue : ReachabilityStatus) =
            if reachabilityStatus.Set newValue then
                try
                    reachabilitySubject.OnNext(newValue)
                with
                    :? ObjectDisposedException -> () // Again, a race condition exists with disposal.


    member __.ReachabilityObservable = reachabilitySubject :> IObservable<ReachabilityStatus>
