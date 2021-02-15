using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{Description}")]
    [DataContract]
    public sealed class EnvironmentalContext : IEquatable<EnvironmentalContext>
    {
        [DataMember]
        private readonly double _waterTemp;
        [DataMember]
        private readonly Salinity _salinity;
        [DataMember]
        private readonly Velocity _speedOfSound;

        private static Lazy<EnvironmentalContext> _default = new Lazy<EnvironmentalContext>(CreateDefaultValue);

        // Parameterless ctor for serialization.
        private EnvironmentalContext() { }

        public EnvironmentalContext(double waterTemp, Salinity salinity, Velocity speedOfSound)
        {
            _waterTemp = waterTemp;
            _salinity = salinity;
            _speedOfSound = speedOfSound;
        }

        public double WaterTemp { get { return _waterTemp; } }
        public Salinity Salinity { get { return _salinity; } }
        public Velocity SpeedOfSound { get { return _speedOfSound; } }

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

        public static bool operator ==(EnvironmentalContext a, EnvironmentalContext b)
            => !(a is null) && a.Equals(b);

        public static bool operator !=(EnvironmentalContext a, EnvironmentalContext b)
            => (a is null) || !a.Equals(b);

        public override int GetHashCode()
            => _waterTemp.GetHashCode() ^ _salinity.GetHashCode() ^ _speedOfSound.GetHashCode();

        public override string ToString()
        {
            return Description;
        }

        public string Description => $"(WaterTemp={WaterTemp}; Salinity={Salinity}; SpeedOfSound={SpeedOfSound})";

        /// <summary>
        /// Handy for debugging whether we have valid environment data yet.
        /// </summary>
        public static EnvironmentalContext Default { get { return _default.Value; } }

        private static EnvironmentalContext CreateDefaultValue()
        {
            return new EnvironmentalContext(
                waterTemp: 15,
                salinity: Salinity.Brackish,
                speedOfSound: Velocity.FromMetersPerSecond(1450));
        }
    }
}
