using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

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
            string serialized = JsonConvert.SerializeObject(original);
            Distance actual = JsonConvert.DeserializeObject<Distance>(serialized);

            Console.WriteLine($"serialized=[{serialized}]");

            Assert.AreEqual(expected, actual);
        }
    }
}
