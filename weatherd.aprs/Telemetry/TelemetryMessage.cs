namespace weatherd.aprs.telemetry
{
    using metrics;

    public abstract class TelemetryMessage : APRSISMessage
    {
        public MetricSet MetricSet { get; }

        protected TelemetryMessage(string callsign, MetricSet metricSet)
            : base(callsign, "APRS")
        {
            TypeIdentifier = string.Empty;
            MetricSet = metricSet;
        }

        protected TelemetryMessage(TelemetryMessage other)
            : base(other.SourceCallsign, "APRS")
        {
            TypeIdentifier = string.Empty;
            MetricSet = other.MetricSet;
        }

        public override string Compile() => $"{base.Compile()}{TypeIdentifier}";
    }
}
