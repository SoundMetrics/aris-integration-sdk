using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public sealed class DistanceJsonTest
    {
        [TestMethod]
        public void RountTripDistance()
        {
            Distance original = Distance.FromMeters(1.25);
            Distance expected = original;
            string serialized = JsonSerializer.Serialize(original);
            Distance actual = JsonSerializer.Deserialize<Distance>(serialized);

            Console.WriteLine($"serialized=[{serialized}]");

            Assert.AreEqual(expected, actual);
        }
    }
}
