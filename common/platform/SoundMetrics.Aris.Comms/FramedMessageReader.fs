// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Serilog
open System
open System.Diagnostics
open System.Net.Sockets
open System.Reactive.Subjects

module private FramedMessageReaderDetails =

    let logCouldNotStartReadForSize (instance : string) (msg : string) =
        Log.Error("Could not start read for size: {msg}; instance: {instance}", msg, instance)

    let logMaxMessageSizeExceeded (instance : string) (msgSize : int) (maxMsgSize : int) =
        Log.Warning("Max message size exceeded: {msgSize}; max={maxMsgSize}; instance: {instance}",
            msgSize, maxMsgSize, instance)

    let logMessagesSizeReadIsIncomplete (instance : string) =
        Log.Warning("Message size read is incomplete; instance: {instance}", instance)

    let logBadSizeRead (instance : string) =
        Log.Error("Bad size read; instance: {instance}", instance)

    let logErrorCompletingSizeRead (instance : string) (msg : string) =
        Log.Information("Couldn't complete size read; possible shutdown: {msg}; instance: {instance}",
            msg, instance)

    let logErrorStartingMessageRead (instance : string) (msg : string) =
        Log.Error("Error starting message read: {msg}; instance: {instance}", msg, instance)

    let logErrorCompletingMessageRead (instance : string) (msg : string) =
        Log.Error("Error completing message read: {msg}; instance: {instance}", msg, instance)


open FramedMessageReaderDetails

/// Reads framed messages from a TCP stream. The message is prefixed
/// by a 4-byte length
type internal FramedMessageReader(tcp : TcpClient, maxBufSize, instanceDescription) =

    let disposed = ref false
    let prefix = Array.zeroCreate<byte> 4
    let msgSubject = new Subject<byte array>()

    let onNext msg =    if not !disposed then msgSubject.OnNext(msg)
    let onCompleted() = if not !disposed then msgSubject.OnCompleted()
    let onError exn =   if not !disposed then msgSubject.OnError(exn)

    let rec startReadSize () =

        try
            tcp.Client.BeginReceive(prefix, 0, prefix.Length, SocketFlags.None,
                AsyncCallback completeReadSize, None) |> ignore
        with
            exn ->  logCouldNotStartReadForSize instanceDescription exn.Message
                    onError exn

    and completeReadSize ar =

        try
            if tcp.Client <> null then // can shutdown while waiting
                let bytesRead = tcp.Client.EndReceive(ar)
                if bytesRead = prefix.Length then
                    let msgSize = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(prefix, 0))
                    if msgSize <= maxBufSize then
                        startReadMsg msgSize
                    else
                        logMaxMessageSizeExceeded instanceDescription msgSize maxBufSize
                        let msg = sprintf "FramedMessageReader: Message size (%d) exceeds maximum size of %d"
                                          msgSize maxBufSize
                        onError (Exception(msg))
                else
                    logBadSizeRead instanceDescription
                    onError (Exception("Incomplete size"))
        with
            exn ->  logErrorCompletingSizeRead instanceDescription exn.Message
                    onError exn

    and startReadMsg size =

        try
            if size = 0 then
                startReadSize()
            else
                let buf = Array.zeroCreate<byte> size
                tcp.Client.BeginReceive(buf, 0, buf.Length, SocketFlags.None,
                    AsyncCallback completeReadMsg, buf) |> ignore
        with
            exn ->  logErrorStartingMessageRead instanceDescription exn.Message
                    onError exn

    and completeReadMsg ar =

        try
            tcp.Client.EndReceive(ar) |> ignore
            let buf = ar.AsyncState :?> byte array
            onNext buf
            startReadSize()
        with
            exn ->  logErrorCompletingMessageRead instanceDescription exn.Message
                    onError exn

    do
        startReadSize()

    interface IDisposable with
        member __.Dispose() =
            Dispose.theseWith disposed [ msgSubject ]
                (fun () -> onCompleted())

    member s.Dispose() = (s :> IDisposable).Dispose()

    member __.Messages = msgSubject :> IObservable<byte array>

