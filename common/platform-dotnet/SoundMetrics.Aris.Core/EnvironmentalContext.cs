using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{Description}")]
    public sealed class EnvironmentalContext
    {
        private readonly double _waterTemp;
        private readonly double _salinity;
        private readonly Velocity _speedOfSound;
        private static Lazy<EnvironmentalContext> _default = new Lazy<EnvironmentalContext>(CreateDefaultValue);

        public EnvironmentalContext(double waterTemp, double salinity, Velocity speedOfSound)
        {
            _waterTemp = waterTemp;
            _salinity = salinity;
            _speedOfSound = speedOfSound;
            IsDefault = false;
        }

        public double WaterTemp { get { return _waterTemp; } }
        public double Salinity { get { return _salinity; } }
        public Velocity SpeedOfSound { get { return _speedOfSound; } }

        public bool IsDefault { get; private set; }

        public override string ToString()
        {
            return Description;
        }

        public string Description
        {
            get
            {
                var baseValue = IsDefault ? "Default" : "";
                return $"{baseValue}(WaterTemp={WaterTemp}; Salinity={Salinity}; SpeedOfSound={SpeedOfSound})";
            }
        }

        /// <summary>
        /// Handy for debugging whether we have valid environment data yet.
        /// </summary>
        public static EnvironmentalContext Default { get { return _default.Value; } }

        private static EnvironmentalContext CreateDefaultValue()
        {
            return new EnvironmentalContext(
                waterTemp: 15,
                salinity: 15,
                speedOfSound: Velocity.FromMetersPerSecond(1450))
            {
                IsDefault = true
            };
        }
    }
}
