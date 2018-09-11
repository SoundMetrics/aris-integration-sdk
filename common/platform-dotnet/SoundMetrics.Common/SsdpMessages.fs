// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Common

    (*
        This file defines SSDP messages.
        See https://tools.ietf.org/html/draft-cai-ssdp-v1-03
    *)

module SsdpMessages =
    open Serilog
    open System
    open System.Net
    open System.Net.Sockets
    open System.Text

    type internal MsgReceived = { UdpResult : UdpReceiveResult; Timestamp : DateTimeOffset }

    type CacheControl = Dummy

    /// Traits of a received SSDP message.
    type SsdpMessageTraits = {
        /// The payload of the message converted to a string.
        RawContent      : string

        /// The time at which the message was received.
        Timestamp       : DateTimeOffset

        /// The origin of the message.
        RemoteEndPoint  : IPEndPoint
    }
    with
        static member internal From(packet : MsgReceived) = 
            { RawContent = Encoding.UTF8.GetString(packet.UdpResult.Buffer)
              Timestamp = packet.Timestamp
              RemoteEndPoint = packet.UdpResult.RemoteEndPoint }

    /// Represents an SSDP NOTIFY message.
    type SsdpNotifyMessage = {
        Host            : IPEndPoint
        CacheControl    : CacheControl
        Location        : IPEndPoint
        NT              : string
        NTS             : string
        Server          : string
        USN             : string
    }

    /// Represents an SSDP M-SEARCH message.
    type SsdpMSearchMessage = {
        Host            : IPEndPoint
        MAN             : string
        MX              : string
        ST              : string
        UserAgent       : string
    }

    /// Discriminated union for types of SSDP messages.
    type SsdpMessage =
        | Notify of SsdpNotifyMessage
        | MSearch of SsdpMSearchMessage
        | Unhandled of content : string

    module internal SsdpMsgDetails =

        /// Fetches the verb from the string--assuming you're sending the contents
        /// of an SSDP message.
        let getVerb (s : string) =

            let idxSpace = s.IndexOfAny([| ' '; '\t' |])
            if idxSpace < 0 then
                Error "No verb found"
            else
                Ok (s.Substring(0, idxSpace).ToUpperInvariant())

        /// Splits a single "name: value" pair passed in.
        let splitNVP (line : string) =
            match line.IndexOf(':') with
            | -1 -> Error line
            | idxColon when line.Length > idxColon + 1 ->
                let name = line.Substring(0, idxColon).Trim()
                let value = line.Substring(idxColon + 1).Trim()
                Ok (name, value)
            | _ -> Error line

        /// Parses a "w.x.y.z:1900"-style endpoint.
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

        /// Parses the name-value pairs from the header of an SSDP message,
        /// skipping the first line and stopping at the first empty line.
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

        /// Applies mapped functions to build a key-value map.
        /// The functions in the map are keyed by name, such as HOST.
        /// The state is the type of message being built.
        let inline makeKeyValuePair functionMap state key value =
            if functionMap |> Map.containsKey key then
                let fn = functionMap.[key]
                fn state value
            else
                state

        let private notifyBuilders = // At module scope to create it only once.
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

        /// Parses an SSDP NOTIFY message.
        let private parseNotify (content : string) _msg =
            
            let map = getHeaderValueMap content
            let msg =
                {
                    SsdpNotifyMessage.Host = IPEndPoint(0L, 0)
                    CacheControl = Dummy
                    Location = IPEndPoint(0L, 0)
                    NT = ""; NTS = ""; Server = ""; USN = ""
                }
            Notify (map |> Map.fold (makeKeyValuePair notifyBuilders) msg)

        let private mSearchBuilders =  // At module scope to create it only once.
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

        /// Parses an SSDP M-SEARCH message.
        let private parseMSearch (content : string) _msg =

            let map = getHeaderValueMap content
            let msg =
                {
                    SsdpMSearchMessage.Host = IPEndPoint(0L, 0)
                    MAN = ""; MX = ""; ST = ""; UserAgent = ""
                }
            MSearch (map |> Map.fold (makeKeyValuePair mSearchBuilders) msg)

        let private parseUnknown (content : string) (msg : MsgReceived) : SsdpMessage =
            Unhandled (Encoding.UTF8.GetString(msg.UdpResult.Buffer))

        let private verbParserMap =
            [
                "NOTIFY",   parseNotify
                "M-SEARCH", parseMSearch
            ]
            |> Map.ofList

        let private verbToParser verb =

            if verbParserMap |> Map.containsKey verb then
                Ok verbParserMap.[verb]
            else
                Ok parseUnknown

        let parse (msg : MsgReceived) =

            let content = Encoding.UTF8.GetString(msg.UdpResult.Buffer)

            getVerb content
            |> Result.bind verbToParser
            |> Result.map (fun parse -> parse content msg)


    type SsdpMessage with
        static member internal From(packet : MsgReceived) = SsdpMsgDetails.parse packet
