using SoundMetrics.Aris.Core.ApprovalTests;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{Description}")]
    [DataContract]
    public sealed class EnvironmentalContext
        : IEquatable<EnvironmentalContext>, IPrettyPrintable
    {
        [DataMember]
        private readonly double _waterTemp;
        [DataMember]
        private readonly Salinity _salinity;
        [DataMember]
        private readonly Distance _depth;
        [DataMember]
        private readonly Velocity _speedOfSound;

        private static readonly Lazy<EnvironmentalContext> _default =
            new Lazy<EnvironmentalContext>(CreateDefaultValue);

        // Parameterless ctor for serialization.
        private EnvironmentalContext() { }

        public EnvironmentalContext(double waterTemp, Salinity salinity, Distance depth)
        {
            _waterTemp = waterTemp;
            _salinity = salinity;
            _depth = depth;
            _speedOfSound =
                Velocity.FromMetersPerSecond(
                    AcousticMath.CalculateSpeedOfSound(
                        waterTemp, depth.Meters, (double)salinity));
        }

        public double WaterTemp => _waterTemp;
        public Salinity Salinity => _salinity;
        public Distance Depth => _depth;
        public Velocity SpeedOfSound => _speedOfSound;

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
                && this._depth == other._depth
                && this._speedOfSound == other._speedOfSound;
        }

        public static bool operator ==(EnvironmentalContext a, EnvironmentalContext b)
            => !(a is null) && a.Equals(b);

        public static bool operator !=(EnvironmentalContext a, EnvironmentalContext b)
            => (a is null) || !a.Equals(b);

        public override int GetHashCode()
            => _waterTemp.GetHashCode()
                ^ _salinity.GetHashCode()
                ^ _depth.GetHashCode()
                ^ _speedOfSound.GetHashCode();

        public override string ToString()
        {
            return Description;
        }

        public string Description => $"(WaterTemp={WaterTemp}; Salinity={Salinity}; Depth={Depth}; SpeedOfSound={SpeedOfSound})";

        /// <summary>
        /// Handy for debugging whether we have valid environment data yet.
        /// </summary>
        public static EnvironmentalContext Default { get { return _default.Value; } }

        private static EnvironmentalContext CreateDefaultValue()
        {
            var defaultWaterTemp = 15.0;
            var defaultSalinity = Salinity.Brackish;
            var defaultDepth = Distance.FromMeters(1.0);

            return new EnvironmentalContext(defaultWaterTemp, defaultSalinity, defaultDepth);
        }

        PrettyPrintHelper IPrettyPrintable.PrettyPrint(PrettyPrintHelper helper)
        {
            helper.PrintHeading($"{nameof(EnvironmentalContext)}");

            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("WaterTemp", WaterTemp);
                helper.PrintValue("Salinity", Salinity);
                helper.PrintValue("Depth", Depth);
                helper.PrintValue("SpeedOfSound", SpeedOfSound);
            }

            return helper;
        }
    }
}
