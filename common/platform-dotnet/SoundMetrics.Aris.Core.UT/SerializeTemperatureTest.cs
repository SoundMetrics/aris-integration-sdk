using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public sealed class SerializeTemperatureTest
    {
        [TestMethod]
        public void RoundTripTemperature()
        {
            Temperature originalData = (Temperature)21.2;
            string serialized = JsonSerializer.Serialize(originalData);
            Temperature deserialized = JsonSerializer.Deserialize<Temperature>(serialized);

            Console.WriteLine("serialized: " + serialized);

            Assert.AreEqual(originalData, deserialized);
        }
    }
}
