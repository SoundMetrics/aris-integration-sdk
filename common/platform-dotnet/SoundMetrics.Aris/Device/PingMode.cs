using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Device
{
    public struct PingMode
    {
        private PingMode(int integralValue, bool isValid)
        {
            this.integralValue = integralValue;
            this.isValid = isValid;
        }

        public int IntegralValue
        {
            get
            {
                if (isValid)
                {
                    return integralValue;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(
                        "Invalid integral ping mode",
                        nameof(IntegralValue));
                }
            }
        }

        public static readonly PingMode PingMode1 = new PingMode(1, isValid: true);
        public static readonly PingMode PingMode3 = new PingMode(3, isValid: true);
        public static readonly PingMode PingMode6 = new PingMode(6, isValid: true);
        public static readonly PingMode PingMode9 = new PingMode(9, isValid: true);

        public static PingMode From(int integralValue)
        {
            // Parsing invalid integral values as a valid value allows us to
            // manipulate headers with invalid values.
            var isValid = IsValidIntegralValue(integralValue);
            return new PingMode(integralValue, isValid);
        }

        public static PingMode Invalid(int integralValue)
        {
            if (IsValidIntegralValue(integralValue))
            {
                throw new ArgumentOutOfRangeException($"{integralValue} is a valid integral value");
            }

            return new PingMode(integralValue, isValid: false);
        }

        internal bool IsValid => IsValidIntegralValue(this.integralValue);

        private static bool IsValidIntegralValue(int integralValue) =>
            ValidValues.Contains(integralValue);

        public void AssertValid()
        {
            if (!IsValid)
            {
                throw new InvalidSonarConfig($"Invalid ping mode '{integralValue}");
            }
        }

        internal void AssertInvalid()
        {
            if (IsValid)
            {
                throw new InvalidSonarConfig("Expected invalid ping mode");
            }
        }

        private readonly int integralValue;
        private readonly bool isValid;

        private static readonly HashSet<int> ValidValues = new HashSet<int>(new[] { 1, 3, 6, 9 });
    }
}
