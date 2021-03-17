﻿using SoundMetrics.Aris.Core.ApprovalTests;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("{Description}")]
    [DataContract]
    public sealed class ObservedConditions
        : IEquatable<ObservedConditions>, IPrettyPrintable
    {
        [DataMember]
        private readonly double _waterTemp;
        [DataMember]
        private readonly Distance _depth;

        private static readonly Lazy<ObservedConditions> _default =
            new Lazy<ObservedConditions>(CreateDefaultValue);

        // Parameterless ctor for serialization.
        private ObservedConditions() { }

        public ObservedConditions(double waterTemp, Distance depth)
        {
            _waterTemp = waterTemp;
            _depth = depth;
        }

        public double WaterTemp => _waterTemp;
        public Distance Depth => _depth;

        public override bool Equals(object obj) => Equals(obj as ObservedConditions);

        public bool Equals(ObservedConditions other)
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
                && this._depth == other._depth;
        }

        public static bool operator ==(ObservedConditions a, ObservedConditions b)
            => !(a is null) && a.Equals(b);

        public static bool operator !=(ObservedConditions a, ObservedConditions b)
            => (a is null) || !a.Equals(b);

        public override int GetHashCode()
            => _waterTemp.GetHashCode() ^ _depth.GetHashCode();

        public override string ToString()
        {
            return Description;
        }

        public string Description => $"(WaterTemp={WaterTemp}; Depth={Depth})";

        /// <summary>
        /// Handy for debugging whether we have valid environment data yet.
        /// </summary>
        public static ObservedConditions Default { get { return _default.Value; } }

        private static ObservedConditions CreateDefaultValue()
        {
            var defaultWaterTemp = 15.0;
            var defaultDepth = Distance.FromMeters(1.0);

            return new ObservedConditions(defaultWaterTemp, defaultDepth);
        }

        PrettyPrintHelper IPrettyPrintable.PrettyPrint(PrettyPrintHelper helper)
        {
            helper.PrintHeading($"{nameof(ObservedConditions)}");

            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("WaterTemp", WaterTemp);
                helper.PrintValue("Depth", Depth);
            }

            return helper;
        }
    }
}
