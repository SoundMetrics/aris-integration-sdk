// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

(*
    ConduitState maps DeviceCxnState into something a bit more user-friendly; this
    represents the state of the conduit and is not the discrete states used in
    DeviceConnection.
*)

namespace SoundMetrics.Aris.Comms.Internal

open SoundMetrics.Aris.Comms

/// High-level representation of the conduit state suitable
/// for consumption by users. Note that the conduit state
/// never goes to "closed," there would be no more conduit
/// in this case.
type internal ConduitState =
    | TryingToConnect   = 0
    | Connected         = 1
    | TryingToReconnect = 2

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module internal ConduitState =

    let getNextDefenderConduitState currentState input : ConduitState =

        let changeTo (state : ConduitState) = true, state
        let noChange = false, ConduitState.TryingToConnect // state doesn't get used here, just false

        let changeState, newState =
            match currentState with
            | ConduitState.TryingToConnect ->
                match input with
                | DeviceCxnState.Start ->                noChange
                | DeviceCxnState.Connected _ ->          changeTo ConduitState.Connected
                | DeviceCxnState.NotConnected ->         noChange
                | DeviceCxnState.ConnectionRefused _ ->  noChange
                | DeviceCxnState.Closed ->               changeTo ConduitState.TryingToReconnect
            | ConduitState.Connected ->
                match input with
                | DeviceCxnState.Start ->                changeTo ConduitState.TryingToReconnect
                | DeviceCxnState.Connected _ ->          noChange
                | DeviceCxnState.NotConnected ->         changeTo ConduitState.TryingToReconnect
                | DeviceCxnState.ConnectionRefused _ ->  changeTo ConduitState.TryingToReconnect
                | DeviceCxnState.Closed ->               changeTo ConduitState.TryingToReconnect
            | ConduitState.TryingToReconnect ->
                match input with
                | DeviceCxnState.Start ->                noChange
                | DeviceCxnState.Connected _ ->          changeTo ConduitState.Connected
                | DeviceCxnState.NotConnected ->         noChange
                | DeviceCxnState.ConnectionRefused _ ->  noChange
                | DeviceCxnState.Closed ->               changeTo ConduitState.TryingToReconnect
            | _ -> failwithf "Unhandled conduit state: '%A'" currentState

        if changeState then
            newState
        else
            currentState
