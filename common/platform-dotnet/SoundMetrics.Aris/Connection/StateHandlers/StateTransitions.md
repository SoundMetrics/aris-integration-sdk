# Connection State Transitions

This document reflects the state transitions that occur in the Connection state
machine.

State transitions are triggered by input events such as network changes, lack of receipt of frames, time passing, and the `Stop` event.

This state chart shows only the states and state transitions, no events.

```mermaid
%% NOTE: This mermaid diagram can be previewed in VS Code with an appropriate plug-in,
%% such as Markdown Preview Mermaid Support by Matt Bierner.
%% It also functions well within GitHub's markdown viewer.

stateDiagram-v2
    [*] --> WatchingForDevice

    WatchingForDevice: Watching For Device
    WatchingForDevice --> AttemptingConnection

    AttemptingConnection: Attempting Connection
    AttemptingConnection --> Connected
    AttemptingConnection --> DeviceAddressChanged

    Connected --> ConnectionLost
    Connected --> DeviceAddressChanged
    
    DeviceAddressChanged: Device Address Changed
    DeviceAddressChanged --> WatchingForDevice

    ConnectionLost: Connection Lost
    ConnectionLost --> WatchingForDevice
```

There is a `Stop` event that is handled in `StateMachine.DispatchEvent()`. This occurs outside the normal state transitions shown here and terminates the state machine.
