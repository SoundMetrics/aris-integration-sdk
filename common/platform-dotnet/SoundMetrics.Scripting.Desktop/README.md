# SoundMetrics.Scripting.Desktop

This assembly provides support for .NET Framework on the Desktop-specific activities, such as waiting for an action to complete asynchronously while pumping the message queue (via Dispatcher.PushFrame()).

This assembly does not take any dependency on its sibling assemblies. As of this writing (15.7.4), Visual Studio support for package ref in F# project files does not have parity with C# projects' support of the same. Thus, no dependencies helps avoid hair-pulling.

## Namespaces

This project includes its code in the `SoundMetrics.Scripting` namespace for ease of use.

## C# Support

C# code calls through the `DispatcherHelper` class for dispatcher support:

```C#
using SoundMetrics.Scripting;

...

    // Synchronous
    var success = DispatcherHelper.WaitForFuncWithDispatch(fn);
    ...

```

## F# Support

F# code automatically opens the `DispatcherHelper` module:

```F#
open SoundMetrics.Scripting

...

    // Synchronous
    let succcess = waitForFuncWithDispatch(fun ->
        ...
        true
    )

    // Asynchronous workload, but waitForAsyncWithDispatch
    // doesn't return until the workload is done.
    // (For convenience with existing code.)
    let success = waitForAsyncWithDispatch
            (async {
              ...
              return true
            })
```
