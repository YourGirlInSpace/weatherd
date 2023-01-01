using System;

namespace weatherd.aprs.telemetry.metrics
{
    public abstract class TelemetryMetric : IEquatable<TelemetryMetric>
    {
        public readonly string Name;
        public readonly string Unit;

        protected TelemetryMetric(string name, string unit)
        {
            Name = name;
            Unit = unit;
        }

        /// <inheritdoc />
        public bool Equals(TelemetryMetric other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name && Unit == other.Unit;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == GetType() && Equals((TelemetryMetric)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Unit != null ? Unit.GetHashCode() : 0);
            }
        }

        public static bool operator ==(TelemetryMetric left, TelemetryMetric right) => Equals(left, right);

        public static bool operator !=(TelemetryMetric left, TelemetryMetric right) => !Equals(left, right);
    }
}
