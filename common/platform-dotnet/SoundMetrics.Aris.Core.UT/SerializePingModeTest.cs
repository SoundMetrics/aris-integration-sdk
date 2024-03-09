using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public sealed class SerializePingModeTest
    {
        [TestMethod]
        public void JsonRoundTripPingMode()
        {
            var originalData = PingMode.PingMode3;
            var serialized = JsonSerializer.Serialize(originalData);
            var deserialized = JsonSerializer.Deserialize<PingMode>(serialized);

            Console.WriteLine("serialized: " + serialized);

            Assert.AreEqual(originalData, deserialized);
        }
    }
}
