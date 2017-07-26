#include "CppUnitTest.h"
#include "Depth.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace Aris::AcousticMath;

namespace UnitTest
{	

    TEST_CLASS(DepthTests)
    {
    private:

        double kAtmosphere = 14.6959;
        double kMetersPerPSI = 0.702398;

    public:
        
        TEST_METHOD(FreshWaterDepthTest)
        {
            const double pressure = 20.0;
            const uint32_t salinity = 0;
            const double waterTemp = 15;
            const double freshWaterDensity15C = 0.999;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / freshWaterDensity15C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);

            Assert::AreEqual(actual, expected);
        }

        TEST_METHOD(BrackishWaterDepthTest)
        {
            const double pressure = 32.0;
            const uint32_t salinity = 15;
            const double waterTemp = 15;
            const double brackishWaterDensity15C = 1.011;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / brackishWaterDensity15C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);

            Assert::AreEqual(actual, expected);
        }

        TEST_METHOD(SaltWaterDepthTest)
        {
            const double pressure = 65.0;
            const uint32_t salinity = 35;
            const double waterTemp = 15;
            const double saltWaterDensity15C = 1.026;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / saltWaterDensity15C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);
            const bool success = (actual == expected);

            Assert::AreEqual(actual, expected);
        }

        TEST_METHOD(WarmWaterDepthTest)
        {
            const double pressure = 23.0;
            const uint32_t salinity = 35;
            const double waterTemp = 40;
            const double saltWaterDensity30C = 1.022;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / saltWaterDensity30C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);

            Assert::AreEqual(actual, expected);
        }

        TEST_METHOD(WrongWaterDepthTest)
        {
            const double pressure = 37.0;
            const uint32_t salinity = 50;
            const double waterTemp = 10;
            const double saltWaterDensity10C = 1.027;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / saltWaterDensity10C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);
            const bool success = (actual == expected);

            Assert::AreEqual(actual, expected);
        }

    };
}