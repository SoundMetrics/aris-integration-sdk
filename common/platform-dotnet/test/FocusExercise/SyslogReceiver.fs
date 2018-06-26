module SyslogReceiver

open SoundMetrics.Aris.Comms
open System
open System.Net
open System.Net.Sockets
open System.Reactive.Subjects
open System.Threading
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks

module private SampleData =

    let samples = [|
        "[16/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Changing availability from available to busy."
        "[16/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Received command message of type SET_FRAMESTREAM_RECEIVER, length 8"
        "[16/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Sending live sonar data to TCP client UDP port 62847."
        "[16/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Received command message of type SET_FRAMESTREAM_SETTINGS, length 6"
        "[16/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Setting packet delay enable=0 period=0"
        "[16/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Received command message of type SET_SALINITY, length 4"
        "[19/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Setting salinity=0"
        "[16/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Received command message of type SET_FOCUS, length 7"
        "[17/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Received focus command with targetPosFU=197; targetPosMC=1616;"
        "[16/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Received command message of type SET_ACOUSTICS, length 36"
        "[19/6 ARIS 24] May 31 13:49:51 arisapp.arm-opt: Apply acoustic settings: cookie=2 frame_period=1000000 samples_per_channel=786 sample_start_delay=10338 cycle_period=2642 beam_sample_period=20 pulse_width=9 enable_xmit=1 frequency_select=0 system_type=2"
        "[17/6 ARIS 24] May 31 13:49:52 arisapp.arm-opt: focus_state=2 current_position=1543"
        "[16/6 ARIS 24] May 31 13:49:53 arisapp.arm-opt: Received command message of type SET_DATETIME, length 24"
        "[19/6 ARIS 24] May 31 13:49:53 arisapp.arm-opt: Sonar system date and time set to 2018-May-31 13:49:53"
        "[16/6 ARIS 24] May 31 13:49:54 arisapp.arm-opt: Received command message of type SET_ACOUSTICS, length 36"
        "[19/6 ARIS 24] May 31 13:49:54 arisapp.arm-opt: Apply acoustic settings: cookie=9 frame_period=1000000 samples_per_channel=786 sample_start_delay=10338 cycle_period=2642 beam_sample_period=20 pulse_width=9 enable_xmit=1 frequency_select=0 system_type=2"
        "[16/6 ARIS 24] May 31 13:49:54 arisapp.arm-opt: Received command message of type SET_FOCUS, length 7"
        "[17/6 ARIS 24] May 31 13:49:54 arisapp.arm-opt: Received focus command with targetPosFU=197; targetPosMC=1616;"
        "[17/6 ARIS 24] May 31 13:49:55 arisapp.arm-opt: focus_state=2 current_position=1701"
        "[16/6 ARIS 24] May 31 13:49:55 arisapp.arm-opt: Received command message of type SET_ACOUSTICS, length 36"
        "[19/6 ARIS 24] May 31 13:49:55 arisapp.arm-opt: Apply acoustic settings: cookie=10 frame_period=215745 samples_per_channel=786 sample_start_delay=10338 cycle_period=2642 beam_sample_period=20 pulse_width=9 enable_xmit=1 frequency_select=0 system_type=2"
        "[16/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: Terminating TCP session on result code: 2"
        "[16/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: TCP session shutdown complete."
        "[16/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: TCP server waiting for connection."
        "[16/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: Done closing down UDP frame stream."
        "[19/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: Saved sonar settings file."
        "[19/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: Disable transmit and halt acquisition."
        "[19/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: Apply acoustic settings: cookie=10 frame_period=0 samples_per_channel=786 sample_start_delay=10338 cycle_period=2642 beam_sample_period=20 pulse_width=9 enable_xmit=0 frequency_select=0 system_type=2"
        "[16/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: Disconnected."
        "[16/6 ARIS 24] May 31 13:50:35 arisapp.arm-opt: Changing availability from busy to available."
    |]

type SyslogMessage =
    | ReceivedFocusCommand of targetPosFU : int * targetPosMC : int
    | UpdatedFocusState of state : int * currentPosition : int
    | Other of string
    | None

module private Details =
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
        let mutable syslogMessage  = SyslogMessage.None

        for regex, cvt in messageTypes |> Seq.takeWhile (fun _ -> not isDone) do
            if regex.IsMatch(s) then
                syslogMessage <- regex.Matches(s).[0] |> cvt
                isDone <- true

        syslogMessage

    let printMessage = function
        | Other _ -> ()
        | None -> ()
        | ReceivedFocusCommand (targetPosFU, targetPosMC) -> printfn "ReceivedFocusCommand: targetPosFU=%d; targetPosMC=%d" targetPosFU targetPosMC
        | UpdatedFocusState (state, currentPosition) -> printfn "UpdatedFocusState: state=%d; currentPosition=%d" state currentPosition

open Details

let internal test () =

    let toBuffer (s : string) = Encoding.UTF8.GetBytes(s)

    printfn "Testing..."
    SampleData.samples |> Seq.map (toBuffer >> toMessage)
                       |> Seq.iter printMessage


let mkSyslogSubject () : ISubject<SyslogMessage> * IDisposable =

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

    let disposable = Dispose.makeDisposable
                        (fun () -> 
                            // Stop listening to the socket
                            cts.Cancel ()
                            udp.Close () // Because UdpClient doesn't know about cancellation--may make it throw.
                            doneSignal.Wait ()

                            // Clean up
                            syslogSubject.OnCompleted () )
                        [udp; syslogSubject; cts; doneSignal]

    syslogSubject :> ISubject<SyslogMessage>, disposable
