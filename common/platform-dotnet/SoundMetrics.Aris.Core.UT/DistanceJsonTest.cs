using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public sealed class DistanceJsonTest
    {
        public record TestContainer(Distance Distance);

        [TestMethod]
        public void RountTripDistance()
        {
            TestContainer original = new(Distance.FromMeters(1.25));
            TestContainer expected = original;
            string serialized = JsonConvert.SerializeObject(original);
            TestContainer actual = JsonConvert.DeserializeObject<TestContainer>(serialized);

            Console.WriteLine($"serialized=[{serialized}]");

            Assert.AreEqual(expected, actual);
        }
    }
}
