using System.Linq;

namespace weatherd.aprs.telemetry
{
    using metrics;

    public class TelemetryEquationsMessage : TelemetryMessage
    {
        /// <inheritdoc />
        public TelemetryEquationsMessage(string callsign, MetricSet metricSet)
            : base(callsign, metricSet)
        {
            TypeIdentifier = "";
        }
        
        /// <summary>
        /// Creates a new <see cref="TelemetryEquationsMessage"/> using another telemetry message as a template.
        /// </summary>
        /// <param name="other">The other telemetry message to use as a template.</param>
        public TelemetryEquationsMessage(TelemetryMessage other)
            : base(other)
        { }

        /// <inheritdoc />
        public override string Compile()
        {
            string analogEqns = string.Join(",", MetricSet.AnalogMetrics.Where(n => n != null)
                                       .SelectMany((x, o) => new[] { x.EqnA, x.EqnB, x.EqnC }));

            return $"{base.Compile()}:{SourceCallsign.PadRight(9)}:EQNS.{analogEqns}";
        }
    }
}
