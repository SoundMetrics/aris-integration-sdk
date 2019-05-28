namespace SoundMetrics.Common.UT

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Common
open System.Net

[<TestClass>]
type NetworkInterfaceInfoDetailsTests () =

    let toBinaryString (i : int) =

        let arr = Array.zeroCreate<char> 32
        let mutable current = i
        for i = 0 to 31 do
            let bit = if (i &&& 1) <> 0 then '1' else '0'
            arr.[i] <- bit

        "0b" + (String(arr))

    [<TestMethod>]
    member __.``Zero mask in byte arrays`` () =

        let testCases = [|
            0b0000_0000_0000_0000, // input
            0b0000_0000_0000_0000, // mask
            0b0000_0000_0000_0000  // expected

            0b0000_0000_0000_0000,
            0b1111_1111_1111_1111,
            0b0000_0000_0000_0000

            0b1000_0100_0010_0001,
            0b0000_0000_0000_0000,
            0b0000_0000_0000_0000

            0b1000_0100_0010_0001,
            0b1111_0000_0000_0000,
            0b1000_0000_0000_0000

            0b1000_0100_0010_0001,
            0b0000_1111_0000_0000,
            0b0000_0100_0000_0000

            0b1000_0100_0010_0001,
            0b0000_0000_1111_0000,
            0b0000_0000_0010_0000

            0b1000_0100_0010_0001,
            0b0000_0000_0000_1111,
            0b0000_0000_0000_0001
        |]

        let toBytes i = BitConverter.GetBytes(i : int)

        let areEqual (a: byte array) (b: byte array) =

            if a.Length <> b.Length then
                false
            else
                let equalCount =
                    a |> Seq.zip b
                      |> Seq.filter (fun (c,d) -> c = d)
                      |> Seq.length
                equalCount = a.Length

        let numberedCases = testCases |> Seq.mapi (fun i t -> i+1,t)

        for i, (a, b, expected) in numberedCases do
            let aBytes = a |> toBytes
            let bBytes = b |> toBytes
            let expectedBytes = expected |> toBytes
            let actualBytes = NetworkInterfaceInfo.Mask(aBytes, bBytes)

            printfn "Case %d" i
            printfn "  a=%s" (toBinaryString a)
            printfn "  b=%s" (toBinaryString b)
            printfn "  e=%s" (toBinaryString (BitConverter.ToInt32(expectedBytes, 0)))
            printfn "  f=%s" (toBinaryString (BitConverter.ToInt32(actualBytes, 0)))

            let msg = sprintf "case %d" i

            Assert.IsTrue(areEqual expectedBytes actualBytes, msg)

    [<TestMethod>]
    member __.``Mask IPAddress`` () =

        let testCases = [|
            "192.168.10.12",
            "255.255.255.0",  // mask
            "192.168.10.0"
        |]

        let numberedCases = testCases |> Seq.mapi (fun i t -> i+1,t)

        for i, (a, mask, expected) in numberedCases do
            let a' = IPAddress.Parse(a)
            let mask' = IPAddress.Parse(mask)
            let expected' = IPAddress.Parse(expected)

            let actual = NetworkInterfaceInfo.Mask(a', mask')

            let msg = sprintf "Case %d" i
            Assert.AreEqual<IPAddress>(expected', actual)

    [<TestMethod>]
    member __.``Is reachable`` () =

        let testCases = [|
            "192.168.10.12",
            "192.168.10.1",  // subnet
            "255.255.255.0", // mask
            true

            "10.11.12.13",
            "10.11.12.1",
            "255.0.0.0",
            true

            "192.168.10.12",
            "10.11.12.1",
            "255.0.0.0",
            false
        |]

        let numberedCases = testCases |> Seq.mapi (fun i t -> i+1,t)

        for i, (addr, subnet, mask, expected) in numberedCases do
            let addr' = IPAddress.Parse(addr)
            let subnet' = IPAddress.Parse(subnet)
            let mask' = IPAddress.Parse(mask)
            let actual = NetworkInterfaceInfo.IsReachable(addr', subnet', mask')

            printfn "case %d" i
            printfn "addr=%A" addr
            printfn "subnet=%A" subnet
            printfn "mask=%A" mask
            printfn "addr=%A" addr
            printfn "actual=%A" actual

            let msg = sprintf "Case %d" i
            Assert.AreEqual<bool>(expected, actual, msg)
