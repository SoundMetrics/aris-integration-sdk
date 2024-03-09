using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public sealed class SerializeFineDurationTest
    {
        [TestMethod]
        public void JsonRoundTripFineDuration()
        {
            var originalData = (FineDuration)42;
            var serialized = JsonSerializer.Serialize(originalData);
            var deserialized = JsonSerializer.Deserialize<FineDuration>(serialized);

            Console.WriteLine("serialized: " + serialized);

            Assert.AreEqual(originalData, deserialized);
        }
    }
}
