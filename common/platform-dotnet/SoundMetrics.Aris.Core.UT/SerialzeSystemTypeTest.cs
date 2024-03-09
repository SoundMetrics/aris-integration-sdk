using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public sealed class SerialzeSystemTypeTest
    {
        [TestMethod]
        public void JsonRoundTripSystemType()
        {
            var originalData = SystemType.Aris3000;
            var serialized = JsonSerializer.Serialize(originalData);
            var deserialized = JsonSerializer.Deserialize<SystemType>(serialized);

            Console.WriteLine("serialized: " + serialized);

            Assert.AreEqual(originalData, deserialized);
        }
    }
}
