namespace SoundMetrics.Network.UT

open System
open System.Net.NetworkInformation
open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Network.DeviceReachability.Details

[<TestClass>]
type DeviceReachabilityTest () =

    let now = DateTime.Now;
    let timeoutLimit = TimeSpan.FromSeconds(10.0)

    let log (level: FsmOutputLevel) (msg: string) =
        Console.WriteLine(sprintf "%A: %s" level msg);


    [<TestMethod>]
    member __.RunStates () =

        let testCases = [|
            "Nominal/Okay --(Success)--> Nominal/Okay",
                State.Start, IPStatus.Success, State.Start

            "Nominal/Okay --(TimedOut)--> Start/TimedOut",
                State.Start, IPStatus.TimedOut, Nominal (TimedOut now)

            "Nominal/TimedOut{long ago} --(TimedOut)--> NotReachable",
                Nominal (TimedOut (now.AddDays(-1.0))),
                    IPStatus.TimedOut,
                    NotReachable

            "Nominal/TimedOut{now-1s} --(TimedOut)--> Nominal/Timedout",
                Nominal (TimedOut (now.AddSeconds(-1.0))),
                    IPStatus.TimedOut,
                    Nominal (TimedOut (now.AddSeconds(-1.0)))

            "NotReachable -->(TimedOut)--> NotReachable",
                NotReachable, IPStatus.TimedOut, NotReachable

            "NotReachable -->(Success)--> Nominal/Okay",
                NotReachable, IPStatus.Success, State.Start
        |]

        let handleEvent = createEventHandler timeoutLimit

        for (idx, el) in testCases |> Seq.mapi (fun i t -> i, t) do

            printfn "------------------------------------------------------------"
            let (description, startState, ev, expected) = el
            printfn "test index %d: \"%s\"" idx description

            let actual = handleEvent now startState ev

            let msg = sprintf "[%d] comparing %A to %A" idx expected actual
            let sameThing = (expected = actual)
            Assert.IsTrue(sameThing, msg)
