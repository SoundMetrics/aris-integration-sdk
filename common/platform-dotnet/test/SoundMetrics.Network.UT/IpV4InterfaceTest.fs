namespace SoundMetrics.Network.UT

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open System.Net

open SoundMetrics.Network.NetworkSupport

[<TestClass>]
type IpV4InterfaceTest () =

    let mkAddr addr = IPAddress.Parse(addr : string)

    let mkIfc addr netmask =
        {
            Address = mkAddr addr
            SubnetMask = mkAddr netmask
        }

    [<TestMethod>]
    member __.``Is correct net address`` () =

        let expected = mkAddr "169.254.0.0"
        let actual = (mkIfc "169.254.82.206" "255.255.0.0").GetNetworkAddress()
        Assert.AreEqual(expected, actual)

        let expected = mkAddr "192.168.88.0"
        let actual = (mkIfc "192.168.88.1" "255.255.255.0").GetNetworkAddress()
        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.``Is target in subnet`` () =

        let expected = true
        let actual =  (mkIfc "192.168.88.1" "255.255.255.0")
                        .IsTargetInSubnet(mkAddr "192.168.88.2")
        Assert.AreEqual(expected, actual)

        let expected = false
        let actual =  (mkIfc "192.168.88.1" "255.255.255.0")
                        .IsTargetInSubnet(mkAddr "192.168.89.1")
        Assert.AreEqual(expected, actual)

        let expected = true
        let actual =  (mkIfc "169.254.82.206" "255.255.0.0")
                        .IsTargetInSubnet(mkAddr "169.254.83.207")
        Assert.AreEqual(expected, actual)

        let expected = false
        let actual =  (mkIfc "169.254.82.206" "255.255.0.0")
                        .IsTargetInSubnet(mkAddr "169.253.82.206")
        Assert.AreEqual(expected, actual)
