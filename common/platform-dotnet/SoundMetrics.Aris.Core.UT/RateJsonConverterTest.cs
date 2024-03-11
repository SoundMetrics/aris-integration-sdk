using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public sealed class RateJsonConverterTest
    {
        [TestMethod]
        public void RoundTripRate()
        {
            var original = new Rate(3, (FineDuration)5);
            var expected = original;
            var serialized = JsonConvert.SerializeObject(original);
            var actual = JsonConvert.DeserializeObject<Rate>(serialized);

            Console.WriteLine($"serialized=[{serialized}]");

            Assert.AreEqual(expected, actual);
        }
    }
}
