namespace SoundMetrics.Scripting

open System

type SyslogMessage =
    | ReceivedFocusCommand of targetPosFU : int * targetPosMC : int
    | UpdatedFocusState of state : int * currentPosition : int
    | Other of string
    | NoMessage

module private Details =
    open System.Text
    open System.Text.RegularExpressions

    type MessageConverter = Match -> SyslogMessage
    type MessageType = Regex * MessageConverter

    let messageTypes : MessageType array = [|
        Regex(".+Received focus command with targetPosFU=(?<targetPosFU>\d+); targetPosMC=(?<targetPosMC>\d+)"),
            fun m ->    let targetPosFU = m.Groups.["targetPosFU"].Value
                        let targetPosMC = m.Groups.["targetPosMC"].Value
                        ReceivedFocusCommand (int targetPosFU, int targetPosMC)
        Regex(".+focus_state=(?<focusState>\d+) current_position=(?<currentPosition>\d+)"),
            fun m ->    let focusState = m.Groups.["focusState"].Value
                        let currentPosition = m.Groups.["currentPosition"].Value
                        UpdatedFocusState (int focusState, int currentPosition)
        Regex(".+"),
            fun m ->    Other (m.Groups.[0].Value)
    |]

    let toMessage (buffer : byte array) =

        let s = Encoding.UTF8.GetString(buffer)
        let mutable isDone = false
        let mutable syslogMessage  = SyslogMessage.NoMessage

        for regex, cvt in messageTypes |> Seq.takeWhile (fun _ -> not isDone) do
            if regex.IsMatch(s) then
                syslogMessage <- regex.Matches(s).[0] |> cvt
                isDone <- true

        syslogMessage

module private SyslogSubject =
    open Details
    open System.Net
    open System.Net.Sockets
    open System.Reactive.Subjects
    open System.Threading
    open System.Threading.Tasks

    [<CompiledName("MakeSyslogSubject")>]
    let makeSyslogSubject () : IObservable<SyslogMessage> * IDisposable =

        // Shutdown
        let cts = new CancellationTokenSource ()
        let doneSignal = new ManualResetEventSlim ()

        // Get & publish packets
        let syslogSubject = new Subject<SyslogMessage> ()
        let udp = new UdpClient ()

        let rec listen () =

            // Switched to task-based rather than wrapping in Async as sometimes the Async
            // just never completes. And that prevents the process from terminating.

            let task = udp.ReceiveAsync()
            let action = Action<Task<UdpReceiveResult>>(fun t ->
                let keepGoing = t.IsCompleted && not t.IsFaulted && not cts.IsCancellationRequested
                if keepGoing then
                    let udpReceiveResult = task.Result
                    let message = udpReceiveResult.Buffer |> toMessage
                    syslogSubject.OnNext message
                    listen()
                else
                    doneSignal.Set() )
            task.ContinueWith(action) |> ignore

        let reuseAddr = true
        let port = 514
        udp.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuseAddr)
        udp.Client.Bind (new IPEndPoint(IPAddress.Any, port));
        listen()

        let disposables : IDisposable list = [udp; syslogSubject; cts; doneSignal]

        syslogSubject  :> IObservable<SyslogMessage>,
            {
                new IDisposable  with
                    member __.Dispose() =
                        // Stop listening to the socket
                        cts.Cancel ()
                        udp.Close () // Because UdpClient doesn't know about cancellation--may make it throw.
                        doneSignal.Wait ()

                        // Clean up
                        syslogSubject.OnCompleted ()
                
                        disposables |> List.iter (fun d -> d.Dispose())

            }

open SyslogSubject

[<Sealed>]
type SyslogListener () =
    let messages, resources = makeSyslogSubject ()

    interface IDisposable with
        member __.Dispose() =
            // No need to dispose the subject, that's handled above in makeSyslogSubject()
            resources.Dispose()

    member __.Messages : IObservable<SyslogMessage> = messages
