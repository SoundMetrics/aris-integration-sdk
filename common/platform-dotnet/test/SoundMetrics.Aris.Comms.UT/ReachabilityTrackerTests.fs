namespace SoundMetrics.Aris.Comms.UT

open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Aris.Comms
open System
open System.Net
open System.Net.Sockets
open System.Threading

open ReachabilityTrackerFsm
open System.Net.NetworkInformation

type MySynchronizationContext() =

    inherit SynchronizationContext()

    do ()


[<TestClass>]
type ReachabilityTrackerTests () =

    [<TestMethod>]
    member __.``Construct with IPv6 target`` () =
        let ipv6TargetAddress = IPAddress.IPv6Loopback
        let synchronizationContext = MySynchronizationContext()

        Assert.IsNotNull(synchronizationContext)

        Assert.ThrowsException<ArgumentException>(fun () ->
                new ReachabilityTracker(ipv6TargetAddress, synchronizationContext) |> ignore )
            |> ignore


    [<TestMethod>]
    member __.``Construct with null SynchronizationContext`` () =
        let targetAddress = IPAddress.Loopback

        Assert.AreEqual(AddressFamily.InterNetwork, targetAddress.AddressFamily)
        Assert.ThrowsException<ArgumentNullException>(fun () ->
                new ReachabilityTracker(targetAddress, null) |> ignore)
            |> ignore

    [<TestMethod>]
    member __.``State transition tests`` () =

        let oneSecond = TimeSpan.FromSeconds(1.0)
        let t1 = DateTimeOffset(2000, 1, 1, 1, 0, 0, TimeSpan.Zero)
        let t2 = t1 + oneSecond
        let t3 = t2 + oneSecond
        let t4 = t3 + oneSecond
        let timeout = TimeSpan.FromSeconds(2.0)

        let testCases =
            [|
                "Nominal/Okay/Success",
                    (Nominal Okay), t1, IPStatus.Success,
                    (Nominal Okay)
                "Nominal-Okay/TimedOut",
                    (Nominal Okay), t1, IPStatus.TimedOut,
                    (Nominal (TimedOut t1))
                "Nominal-Okay/TimedOut",
                    (Nominal Okay), t4, IPStatus.TimedOut,
                    (Nominal (TimedOut t4))
                "Nominal-TimedOut/Success",
                    (Nominal (TimedOut t1)), t4, IPStatus.Success,
                    (Nominal Okay)
                "Nominal-TimedOut/TimedOut",
                    (Nominal (TimedOut t1)), t4, IPStatus.TimedOut,
                    NotReachable
                "NotReachable/TimedOut",
                    NotReachable, t1, IPStatus.TimedOut,
                    NotReachable
                "NotReachable/Success",
                    NotReachable, t1, IPStatus.Success,
                    (Nominal Okay)
            |]
            |> Array.mapi (fun i inputs -> (i + 1, inputs))

        for (i, (description, startState, time, ipStatus, expected)) in testCases do
            printfn "Running %d: '%s'" i description
            let actual = ReachabilityTrackerDetails.handleEvent timeout time startState ipStatus
            Assert.AreEqual(expected, actual, sprintf "In test %d: '%s'" i description)
