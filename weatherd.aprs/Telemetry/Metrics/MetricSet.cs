using System;

namespace weatherd.aprs.telemetry.metrics
{
    public class MetricSet
    {
        public AnalogTelemetryMetric[] AnalogMetrics { get; }

        public BinaryTelemetryMetric[] DigitalMetrics { get; }

        public MetricSet()
        {
            AnalogMetrics = new AnalogTelemetryMetric[5];
            DigitalMetrics = new BinaryTelemetryMetric[8];
        }

        public MetricSet(AnalogTelemetryMetric[] analogMetrics, BinaryTelemetryMetric[] digitalMetrics)
            : this()
        {
            if (analogMetrics == null)
                throw new ArgumentNullException(nameof(analogMetrics));
            if (digitalMetrics == null)
                throw new ArgumentNullException(nameof(digitalMetrics));
            if (analogMetrics.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(analogMetrics));
            if (digitalMetrics.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(digitalMetrics));
            if (analogMetrics.Length > 5)
                throw new ArgumentException("You may only have 5 analog metrics in a metric set.", nameof(analogMetrics));
            if (digitalMetrics.Length > 8)
                throw new ArgumentException("You may only have 8 digital metrics in a metric set.", nameof(digitalMetrics));

            for (int i = 0; i < Math.Min(analogMetrics.Length, 5); i++)
                AnalogMetrics[i] = analogMetrics[i];
            for (int i = 0; i < Math.Min(digitalMetrics.Length, 7); i++)
                DigitalMetrics[i] = digitalMetrics[i];
        }
    }
}
