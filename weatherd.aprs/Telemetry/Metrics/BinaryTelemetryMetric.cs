namespace weatherd.aprs.telemetry.metrics
{

    public class BinaryTelemetryMetric : TelemetryMetric
    {
        public BinaryTelemetryMetric(string name)
            : base(name, string.Empty)
        { }

        public BinaryTelemetryMetric(string name, string unit)
            : base(name, unit)
        { }
    }
}
