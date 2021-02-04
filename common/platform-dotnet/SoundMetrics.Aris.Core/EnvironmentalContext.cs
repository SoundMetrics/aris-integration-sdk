using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{Description}")]
    public sealed class EnvironmentalContext : IEquatable<EnvironmentalContext>
    {
        private readonly double _waterTemp;
        private readonly Salinity _salinity;
        private readonly Velocity _speedOfSound;
        private static Lazy<EnvironmentalContext> _default = new Lazy<EnvironmentalContext>(CreateDefaultValue);

        public EnvironmentalContext(double waterTemp, Salinity salinity, Velocity speedOfSound)
        {
            _waterTemp = waterTemp;
            _salinity = salinity;
            _speedOfSound = speedOfSound;
            IsDefault = false;
        }

        public double WaterTemp { get { return _waterTemp; } }
        public Salinity Salinity { get { return _salinity; } }
        public Velocity SpeedOfSound { get { return _speedOfSound; } }

        public bool IsDefault { get; private set; }

        public override bool Equals(object obj) => Equals(obj as EnvironmentalContext);

        public bool Equals(EnvironmentalContext other)
        {
            if (other is null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return this._waterTemp == other._waterTemp
                && this._salinity == other._salinity
                && this._speedOfSound == other._speedOfSound;
        }

        public override int GetHashCode()
            => _waterTemp.GetHashCode() ^ _salinity.GetHashCode() ^ _speedOfSound.GetHashCode();

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
                salinity: Salinity.Brackish,
                speedOfSound: Velocity.FromMetersPerSecond(1450))
            {
                IsDefault = true
            };
        }
    }
}
