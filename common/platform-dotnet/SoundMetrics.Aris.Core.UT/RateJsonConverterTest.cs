using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

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
            var serialized = JsonSerializer.Serialize(original);
            var actual = JsonSerializer.Deserialize<Rate>(serialized);

            Console.WriteLine($"serialized=[{serialized}]");

            Assert.AreEqual(expected, actual);
        }
    }
}
