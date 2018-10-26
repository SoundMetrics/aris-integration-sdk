// Copyright 2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Network

    (*
        This file defines SSDP messages.
        See https://tools.ietf.org/html/draft-cai-ssdp-v1-03
    *)

module SsdpMessages =
    open Serilog
    open System
    open System.Diagnostics
    open System.Net
    open System.Net.Sockets
    open System.Text

    /// Container for a received packet and a timestamp.
    type internal MsgReceived = { UdpResult : UdpReceiveResult; LocalEndPoint : IPEndPoint; Timestamp : DateTimeOffset }

    type CacheControl = Empty // TODO ############################

    //-------------------------------------------------------------------------
    // SSDP Messages

    /// Traits of a received SSDP message.
    type SsdpMessageProperties = {
        /// The payload of the message converted to a string.
        RawContent      : string

        /// The time at which the message was received.
        Timestamp       : DateTimeOffset

        /// The endpoint receiving the message.
        LocalEndPoint   : IPEndPoint

        /// The origin of the message.
        RemoteEndPoint  : IPEndPoint

        /// Stopwatch for measuring performance during development.
        Stopwatch : Stopwatch
    }
    with
        static member internal From(packet : MsgReceived) = 
            { RawContent = Encoding.UTF8.GetString(packet.UdpResult.Buffer)
              Timestamp = packet.Timestamp
              LocalEndPoint = packet.LocalEndPoint
              RemoteEndPoint = packet.UdpResult.RemoteEndPoint
              Stopwatch = Stopwatch.StartNew() }
        static member internal From(packet : byte array, timestamp, localEP, remoteEP) =
            { RawContent = Encoding.UTF8.GetString(packet)
              Timestamp = timestamp
              LocalEndPoint = localEP
              RemoteEndPoint = remoteEP
              Stopwatch = Stopwatch.StartNew() }

    /// Represents an SSDP NOTIFY message.
    type SsdpNotifyMessage = {
        Host            : IPEndPoint
        CacheControl    : CacheControl
        Location        : string // generally a valid URL
        ST              : string
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
        | Response of SsdpNotifyMessage
        | MSearch of SsdpMSearchMessage
        | Unhandled of content : string

    //-------------------------------------------------------------------------
    // SerDes

    module private SsdpMsgDeserialize =

        // Parse message headers ----------------------------------------------

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

            let allLines = content.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
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

        (*---------------------------------------------------------------------

            Message Object Construction

            We construct SSDP message objects from a received packet. The
            packet looks something like this:

                NOTIFY * HTTP/1.1\r\n
                Host: 239.255.255.250:1900\r\n
                Cache-Control: max-age=4\r\n
                Location: 192.168.10.164:55456\r\n
                NT: uuid:4E50646A-B607-4ECB-9676-8DC10ABE8A5F\r\n
                NTS: "ssdp:alive"\r\n
                SERVER: windows/6.2 IntelUSBoverIP:1/1\r\n
                USN: uuid:4E50646A-B607-4ECB-9676-8DC10ABE8A5F::IntelUSBoverIP:1\r\n
                \r\n
                ...

            In order to parse that into an object, we

                1.  Determine the message type from the first line (NOTIFY).

                2.  Parse the remaining lines, up to the first empty line, as
                    name-value pairs.

                3.  Using a dictionary of field setter functions, fold over the
                    name-value pairs and, if there is a setter function available
                    for that field, use it to create an updated object.

                4.  The output of the fold is the object representation of the message.
        *)


        // Helper function for applying a fold to a key-value map
        // (args key and value are an entry in this map).
        // The key and value args are from a received SSDP message.
        // The state of the fold is the message being built.
        // If the key, e.g., HOST, is in the function map, the function
        // for HOST is applied and creates a new state (object).
        //
        // (The functions in the map are keyed by name, such as HOST,
        // for which the value is probably 239.255.255.250:1900.)
        let inline applyValueToMsg functionMap state key value =
            if functionMap |> Map.containsKey key then
                let fn = functionMap.[key]
                fn state value
            else
                state

        //---------------------------------------------------------------------
        // Parse the NOTIFY message

        let notifyFieldSetters = // At module scope to create it only once.
            [
                "HOST",     fun msg value ->
                                match parseEndPoint value with
                                | Some ep -> { msg with SsdpNotifyMessage.Host = ep }
                                | None -> msg
                "CACHE-CONTROL",
                            fun msg value -> msg // TODO #######################
                "LOCATION", fun msg value -> { msg with Location = value }
                "NT",       fun msg value -> { msg with ST = value }
                "NTS",      fun msg value -> { msg with NTS = value }
                "SERVER",   fun msg value -> { msg with Server = value }
                "USN",      fun msg value -> { msg with USN = value }

            ]
            |> Map.ofList

        let parseNotifyContents (content : string) =

            let headerValues = getHeaderValueMap content
            let initialState =
                {
                    SsdpNotifyMessage.Host = IPEndPoint(0L, 0)
                    CacheControl = Empty
                    Location = ""
                    ST = ""; NTS = ""; Server = ""; USN = ""
                }
            headerValues |> Map.fold (applyValueToMsg notifyFieldSetters) initialState

        /// Parses an SSDP NOTIFY message.
        let parseNotify (content : string) =

            let contents = parseNotifyContents content
            Notify contents

        //---------------------------------------------------------------------
        // Parse a response packet

        let parseResponse (packet : byte array) =

            let contents = Encoding.ASCII.GetString(packet) |> parseNotifyContents
            Response contents

        //---------------------------------------------------------------------
        // Parse the M-SEARCH message

        let mSearchFieldSetters =  // At module scope to create it only once.
            [
                "HOST",     fun msg value ->
                                match parseEndPoint value with
                                | Some ep -> { msg with SsdpMSearchMessage.Host = ep }
                                | None -> msg
                "MAN",      fun msg value -> { msg with MAN = value }
                //"MX",       fun msg value -> { msg with MX = value } // TODO ????
                "ST",       fun msg value -> { msg with ST = value }
                "USER-AGENT", fun msg value -> { msg with UserAgent = value }
            ]
            |> Map.ofList

        /// Parses an SSDP M-SEARCH message.
        let parseMSearch (content : string) =

            let headerValues = getHeaderValueMap content
            let initialState =
                {
                    SsdpMSearchMessage.Host = IPEndPoint(0L, 0)
                    MAN = ""; MX = ""; ST = ""; UserAgent = ""
                }
            MSearch (headerValues |> Map.fold (applyValueToMsg mSearchFieldSetters) initialState)

        /// Parses message types we're not handling.
        let parseUnknown (content : string) : SsdpMessage = Unhandled content

        let verbParserMap =
            [
                "NOTIFY",   parseNotify
                "M-SEARCH", parseMSearch
            ]
            |> Map.ofList

        let verbToParser verb =

            if verbParserMap |> Map.containsKey verb then
                Ok verbParserMap.[verb]
            else
                Ok parseUnknown

        let deserialize (msg : MsgReceived) =

            let content = Encoding.UTF8.GetString(msg.UdpResult.Buffer)

            getVerb content
            |> Result.bind verbToParser
            |> Result.map (fun parse -> parse content)


    module private SsdpMsgSerialize =
        let CRLF = "\r\n"

        let serializeNotify (msg : SsdpNotifyMessage) =

            let s =
                "NOTIFY * HTTP/1.1" + CRLF
                + (sprintf "Host:%s:%d" (msg.Host.Address.ToString()) msg.Host.Port) + CRLF
                + (sprintf "NT:%s" msg.ST) + CRLF
                + (sprintf "NTS:%s" msg.NTS) + CRLF
                + (sprintf "Location:%s" msg.Location) + CRLF
                + (sprintf "USN:%s" msg.USN) + CRLF
                // Skipping cache-control for now.
                + (sprintf "Server:%s" msg.Server) + CRLF
                + CRLF
            Encoding.ASCII.GetBytes(s)

        let serializeResponse (msg : SsdpNotifyMessage) =

            let s =
                "HTTP/1.1 200 OK" + CRLF
                + (sprintf "Host:%s:%d" (msg.Host.Address.ToString()) msg.Host.Port) + CRLF
                + (sprintf "NT:%s" msg.ST) + CRLF
                + (sprintf "NTS:%s" msg.NTS) + CRLF
                + (sprintf "Location:%s" msg.Location) + CRLF
                + (sprintf "USN:%s" msg.USN) + CRLF
                // Skipping cache-control for now.
                + (sprintf "Server:%s" msg.Server) + CRLF
                + CRLF
            Encoding.ASCII.GetBytes(s)

        let serializeMSearch (msg : SsdpMSearchMessage) =

            let s =
                "M-SEARCH * HTTP/1.1" + CRLF
                + (sprintf "Host:%s:%d" (msg.Host.Address.ToString()) msg.Host.Port) + CRLF
                + (sprintf "ST:%s" msg.ST) + CRLF
                + (sprintf "MAN:%s" msg.MAN) + CRLF
                + (sprintf "USER-AGENT:%s" msg.UserAgent) + CRLF
                + CRLF
            Encoding.ASCII.GetBytes(s)

        let serialize = function
            | Notify msg ->     serializeNotify msg
            | Response msg ->   serializeResponse msg
            | MSearch msg ->    serializeMSearch msg
            | Unhandled _ ->    failwith "not supported"

    //-------------------------------------------------------------------------
    // SerDes helpers

    open SsdpMsgSerialize
    open SsdpMsgDeserialize

    type SsdpMessage with
        static member internal FromMulticast(packet : MsgReceived) : Result<SsdpMessage, string> =
            
            deserialize packet

        static member internal FromResponse (packet : byte array) : SsdpMessage =

            parseResponse packet

        static member internal ToPacket (message : SsdpMessage) : byte array =

            serialize message
