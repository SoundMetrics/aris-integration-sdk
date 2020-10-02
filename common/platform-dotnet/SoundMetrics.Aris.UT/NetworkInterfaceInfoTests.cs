using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Network;
using System;
using System.Linq;
using System.Net;

namespace SoundMetrics.Aris.UT
{
    [TestClass]
    public class NetworkInterfaceInfoTests
    {
        private static string ToBinaryString(int i)
        {
            var arr = new char[32];
            var current = i;

            for (int idx = 0; idx < 32; ++idx)
            {
                var bit = (current & 1) == 1 ? '1' : '0';
                arr[idx] = bit;
                current >>= 1;
            }

            return "0b" + new string(arr);
        }

        [TestMethod]
        public void ZeroMaskInByteArrays()
        {
            var testCases = new ValueTuple<int,int,int>[]
                {
                    (0b0000_0000_0000_0000, // input
                    0b0000_0000_0000_0000,  // mask
                    0b0000_0000_0000_0000), // expected

                    (0b0000_0000_0000_0000,
                    0b1111_1111_1111_1111,
                    0b0000_0000_0000_0000),

                    (0b1000_0100_0010_0001,
                    0b0000_0000_0000_0000,
                    0b0000_0000_0000_0000),

                    (0b1000_0100_0010_0001,
                    0b1111_0000_0000_0000,
                    0b1000_0000_0000_0000),

                    (0b1000_0100_0010_0001,
                    0b0000_1111_0000_0000,
                    0b0000_0100_0000_0000),

                    (0b1000_0100_0010_0001,
                    0b0000_0000_1111_0000,
                    0b0000_0000_0010_0000),

                    (0b1000_0100_0010_0001,
                    0b0000_0000_0000_1111,
                    0b0000_0000_0000_0001),
                };

            byte[] ToBytes(int i) => BitConverter.GetBytes(i);

            var numberedCases =
                Enumerable.Range(1, testCases.Length).Zip(testCases);

            foreach (var (i, (a, b, expected)) in numberedCases)
            {
                var aBytes = ToBytes(a);
                var bBytes = ToBytes(b);
                var expectedBytes = ToBytes(expected);
                var actualBytes = NetworkInterfaceInfo.Mask(aBytes, bBytes);

                CollectionAssert.AreEqual(expectedBytes, actualBytes, $"test {i}");
            }
        }

        [TestMethod]
        public void MaskIPAddress()
        {
            var testCases = new[] {
                ("192.168.10.12",
                "255.255.255.0",  // mask
                "192.168.10.0"),
            };

            var numberedCases =
                Enumerable.Range(1, testCases.Length).Zip(testCases);

            foreach (var (i, (a, mask, expected)) in numberedCases)
            {
                var addressA = IPAddress.Parse(a);
                var maskValue = IPAddress.Parse(mask);
                var expectedValue = IPAddress.Parse(expected);
                var actual = NetworkInterfaceInfo.Mask(addressA, maskValue);

                Assert.AreEqual<IPAddress>(expectedValue, actual, $"test {i}");
            }
        }

        [TestMethod]
        public void IsReachable()
        {
            var testCases = new[]
            {
                ("192.168.10.12",
                "192.168.10.1",  // subnet
                "255.255.255.0", // mask
                true),

                ("10.11.12.13",
                "10.11.12.1",
                "255.0.0.0",
                true),

                ("192.168.10.12",
                "10.11.12.1",
                "255.0.0.0",
                false),
            };


            var numberedCases =
                Enumerable.Range(1, testCases.Length).Zip(testCases);

            foreach (var (i, (addr, subnet, mask, expected)) in numberedCases)
            {
                var addrValue = IPAddress.Parse(addr);
                var subnetValue = IPAddress.Parse(subnet);
                var maskValue = IPAddress.Parse(mask);
                var actual = NetworkInterfaceInfo.IsReachable(addrValue, subnetValue, maskValue);

                Assert.AreEqual<bool>(expected, actual, $"test {i}");
            }
        }
    }
}
