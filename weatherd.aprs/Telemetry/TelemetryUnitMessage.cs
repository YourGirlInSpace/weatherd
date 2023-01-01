using System;
using System.Linq;

namespace weatherd.aprs.telemetry
{
    using metrics;

    public class TelemetryUnitMessage : TelemetryMessage
    {
        private readonly int[] maxFieldLengths = { 7, 7, 6, 6, 5, 6, 5, 4, 4, 4, 3, 3, 3 };

        /// <inheritdoc />
        public TelemetryUnitMessage(string callsign, MetricSet metricSet)
            : base(callsign, metricSet)
        {
            TypeIdentifier = "";
        }

        /// <summary>
        /// Creates a new <see cref="TelemetryUnitMessage"/> using another telemetry message as a template.
        /// </summary>
        /// <param name="other">The other telemetry message to use as a template.</param>
        public TelemetryUnitMessage(TelemetryMessage other)
            : base(other)
        { }

        /// <inheritdoc />
        public override string Compile()
        {
            var zip = string.Join(",", MetricSet.AnalogMetrics.Union<TelemetryMetric>(MetricSet.DigitalMetrics.Where(n => n != null)).Zip(maxFieldLengths,
                                                    (metric, len) =>
                                                        metric?.Unit.Substring(0, Math.Min(metric.Unit.Length, len)) ?? string.Empty)
                                                // Remove trailing bits
                                                .Reverse()
                                                .SkipWhile(string.IsNullOrEmpty)
                                                .Reverse());

            return $"{base.Compile()}:{SourceCallsign.PadRight(9)}:UNIT.{zip}";
        }
    }
}
