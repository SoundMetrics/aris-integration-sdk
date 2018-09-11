// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

module SsdpMessages =
    open Serilog
    open System
    open System.Net
    open System.Net.Sockets
    open System.Text
    open System.Transactions

    type internal MsgReceived = { UdpResult : UdpReceiveResult; Timestamp : DateTimeOffset }

    type CacheControl = Dummy

    type SsdpMessageTraits = {
        RawContent      : string
        Timestamp       : DateTimeOffset
        RemoteEndPoint  : IPEndPoint
    }
    with
        static member internal From(packet : MsgReceived) = 
            { RawContent = Encoding.UTF8.GetString(packet.UdpResult.Buffer)
              Timestamp = packet.Timestamp
              RemoteEndPoint = packet.UdpResult.RemoteEndPoint }

    type SsdpNotifyMessage = {
        Host            : IPEndPoint
        CacheControl    : CacheControl
        Location        : IPEndPoint
        NT              : string
        NTS             : string
        Server          : string
        USN             : string
    }

    type SsdpMSearchMessage = {
        Host            : IPEndPoint
        MAN             : string
        MX              : string // TODO ?? ###############
        ST              : string
        UserAgent       : string
    }

    type SsdpMessage =
        | Notify of SsdpNotifyMessage
        | MSearch of SsdpMSearchMessage
        | Unhandled of content : string

    module internal SsdpMsgDetails =

        let getVerb (s : string) =

            let idxSpace = s.IndexOfAny([| ' '; '\t' |])
            if idxSpace < 0 then
                Error "No verb found"
            else
                Ok (s.Substring(0, idxSpace).ToUpperInvariant())

        let splitNVP (line : string) =
            match line.IndexOf(':') with
            | -1 -> Error line
            | idxColon when line.Length > idxColon + 1 ->
                let name = line.Substring(0, idxColon).Trim()
                let value = line.Substring(idxColon + 1).Trim()
                Ok (name, value)
            | _ -> Error line

        let parseEndPoint (s : string) =
            let splits = s.Split(':')
            match splits with
            | [| addr; port |] ->
                let goodAddr, addr = IPAddress.TryParse(addr)
                let goodPort, port = Int32.TryParse(port)
                match struct (goodAddr, goodPort) with
                | struct (true, true) -> Some (IPEndPoint(addr, port))
                | _ -> None
            | _ -> None

        let getHeaderValueMap (content : string) =

            let allLines = content.Split([| '\n'; '\r' |])
            let lines =
                allLines |> Seq.skip 1
                         |> Seq.takeWhile (fun line -> not (String.IsNullOrWhiteSpace(line)))

            let mutable map = Map.empty<_,_>

            for line in lines do
                match splitNVP line with
                | Ok (name, value) ->
                    let name' = name.ToUpperInvariant()
                    map <- map |> Map.add name' value
                | Error msg -> Log.Information("Invalid NVP: {line}", line)

            map

        let inline applyBuilders builderMap state key value =
            if builderMap |> Map.containsKey key then
                let fn = builderMap.[key]
                fn state value
            else
                state

        let notifyBuilders =
            [
                "HOST",     fun msg value ->
                                match parseEndPoint value with
                                | Some ep -> { msg with SsdpNotifyMessage.Host = ep }
                                | None -> msg
                "CACHE-CONTROL",
                            fun msg value -> msg // TODO #######################
                "LOCATION", fun msg value ->
                                match parseEndPoint value with
                                | Some ep -> { msg with Location = ep }
                                | None -> msg
                "NT",       fun msg value -> { msg with NT = value }
                "NTS",      fun msg value -> { msg with NTS = value }
                "SERVER",   fun msg value -> { msg with Server = value }
                "USN",      fun msg value -> { msg with USN = value }

            ]
            |> Map.ofList

        let parseNotify (content : string) _msg =
            
            let map = getHeaderValueMap content
            let msg =
                {
                    SsdpNotifyMessage.Host = IPEndPoint(0L, 0)
                    CacheControl = Dummy
                    Location = IPEndPoint(0L, 0)
                    NT = ""; NTS = ""; Server = ""; USN = ""
                }
            Notify (map |> Map.fold (applyBuilders notifyBuilders) msg)

        let mSearchBuilders =
            [
                "HOST",     fun msg value ->
                                match parseEndPoint value with
                                | Some ep -> { msg with SsdpMSearchMessage.Host = ep }
                                | None -> msg
                "MAN",      fun msg value -> { msg with MAN = value }
                "MX",       fun msg value -> { msg with MX = value }
                "ST",       fun msg value -> { msg with ST = value }
                "USER-AGENT", fun msg value -> { msg with UserAgent = value }
            ]
            |> Map.ofList

        let parseMSearch (content : string) _msg =

            let map = getHeaderValueMap content
            let msg =
                {
                    SsdpMSearchMessage.Host = IPEndPoint(0L, 0)
                    MAN = ""; MX = ""; ST = ""; UserAgent = ""
                }
            MSearch (map |> Map.fold (applyBuilders mSearchBuilders) msg)

        let parseUnknown (content : string) (msg : MsgReceived) : SsdpMessage =
            Unhandled (Encoding.UTF8.GetString(msg.UdpResult.Buffer))

        let verbMap =
            [
                "NOTIFY",   parseNotify
                "M-SEARCH", parseMSearch
            ]
            |> Map.ofList

        let verbToParser verb =

            match verbMap.TryGetValue(verb) with
            | true, parser -> Ok parser
            | false, _ -> Ok parseUnknown

        let parse (msg : MsgReceived) =

            let content = Encoding.UTF8.GetString(msg.UdpResult.Buffer)

            getVerb content
            |> Result.bind verbToParser
            |> Result.map (fun parse -> parse content msg)


    type SsdpMessage with
        static member internal From(packet : MsgReceived) = SsdpMsgDetails.parse packet
