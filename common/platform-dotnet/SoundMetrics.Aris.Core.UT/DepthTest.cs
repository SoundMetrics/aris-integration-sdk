using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SoundMetrics.Aris.Core
{
    using static Depth;

    [TestClass]
    public sealed class DepthTest
    {
        private const double Atmosphere = 14.6959;
        private const double MetersPerPSI = 0.702398;

        [TestMethod]
        public void BrackishWaterDepthTest()
        {
            const double pressure = 32.0;
            const float salinity = 15;
            Temperature waterTemp = (Temperature)15;
            const double brackishWaterDensity15C = 1.011f;
            const double expected = (pressure - Atmosphere) * MetersPerPSI / brackishWaterDensity15C;
            double actual = CalculateDepth(pressure, salinity, waterTemp).Meters;

            Assert.AreEqual(actual, expected);
        }

        [TestMethod]
        public void SaltWaterDepthTest()
        {
            const double pressure = 65.0;
            const float salinity = 35;
            Temperature waterTemp = (Temperature)15;
            const double saltWaterDensity15C = 1.026f;
            const double expected = (pressure - Atmosphere) * MetersPerPSI / saltWaterDensity15C;
            double actual = CalculateDepth(pressure, salinity, waterTemp).Meters;

            Assert.AreEqual(actual, expected);
        }

        [TestMethod] 
        public void WarmWaterDepthTest()
        {
            const double pressure = 23.0;
            const float salinity = 35;
            Temperature waterTemp = (Temperature)40;
            const double saltWaterDensity30C = 1.022f;
            const double expected = (pressure - Atmosphere) * MetersPerPSI / saltWaterDensity30C;
            double actual = CalculateDepth(pressure, salinity, waterTemp).Meters;

            Assert.AreEqual(actual, expected);
        }

        // salinity of 50 PPT is invalid so treat as salt water
        [TestMethod]
        public void WrongWaterDepthTest()
        {
            const double pressure = 37.0;
            const float salinity = 50;
            Temperature waterTemp = (Temperature)10;
            const double saltWaterDensity10C = 1.027f;
            const double expected = (pressure - Atmosphere) * MetersPerPSI / saltWaterDensity10C;
            double actual = CalculateDepth(pressure, salinity, waterTemp).Meters;

            Assert.AreEqual(actual, expected);
        }

        [TestMethod]
        public void LowTempDepthTest()
        {
          const double pressure = 65.0;
          const float salinity = 35;
          Temperature waterTemp = (Temperature)(-2);
          const double saltWaterDensity0C = 1.028f;
          const double expected = (pressure - Atmosphere) * MetersPerPSI / saltWaterDensity0C;
          double actual = CalculateDepth(pressure, salinity, waterTemp).Meters;

          Assert.AreEqual(actual, expected);
        }

        [TestMethod]
        public void HighTempDepthTest()
        {
          const double pressure = 65.0;
          const float salinity = 35;
          Temperature waterTemp = (Temperature)31;
          const double saltWaterDensity30C = 1.022f;
          const double expected = (pressure - Atmosphere) * MetersPerPSI / saltWaterDensity30C;
          double actual = CalculateDepth(pressure, salinity, waterTemp).Meters;

          Assert.AreEqual(actual, expected);
        }
    }
}
