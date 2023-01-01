using System;
using System.Linq;

namespace weatherd.aprs.telemetry
{
    using metrics;

    public class TelemetryParameterMessage : TelemetryMessage
    {
        private readonly int[] maxFieldLengths = { 7, 7, 6, 6, 5, 6, 5, 4, 4, 4, 3, 3, 3 };

        /// <inheritdoc />
        public TelemetryParameterMessage(string callsign, MetricSet metricSet)
            : base(callsign, metricSet)
        {
            TypeIdentifier = "";
        }
        
        /// <summary>
        /// Creates a new <see cref="TelemetryParameterMessage"/> using another telemetry message as a template.
        /// </summary>
        /// <param name="other">The other telemetry message to use as a template.</param>
        public TelemetryParameterMessage(TelemetryMessage other)
            : base(other)
        { }

        /// <inheritdoc />
        public override string Compile()
        {
            var zip = string.Join(",", MetricSet.AnalogMetrics.Union<TelemetryMetric>(MetricSet.DigitalMetrics).Zip(
                                                    maxFieldLengths,
                                                    (metric, len) =>
                                                        metric?.Name.Substring(0, Math.Min(metric.Name.Length, len)) ??
                                                        string.Empty)
                                                // Remove trailing bits
                                                .Reverse()
                                                .SkipWhile(string.IsNullOrEmpty)
                                                .Reverse());

            return $"{base.Compile()}:{SourceCallsign.PadRight(9)}:PARM.{zip}";
        }
    }
}
