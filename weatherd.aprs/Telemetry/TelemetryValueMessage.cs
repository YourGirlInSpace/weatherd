using System;
using System.Linq;
using System.Threading;

namespace weatherd.aprs.telemetry
{
    using metrics;

    public class TelemetryValueMessage : TelemetryMessage
    {
        private static int _globalCounter;

        static TelemetryValueMessage()
        {
            Random random = new Random();
            _globalCounter = random.Next(0, 999);
        }

        public int SequenceNumber { get; private set; }

        public string Comment { get; }

        private readonly float[] _measurements;
        internal FlagsWrapper _flags;
        private bool _invalidated;
        
        public TelemetryValueMessage(string callsign, MetricSet metricSet)
            : base(callsign, metricSet)
        {
            TypeIdentifier = "T";

            SequenceNumber = 0;
            
            _measurements = new float[5];
            _flags = new FlagsWrapper();
            _invalidated = false;
        }
        
        public TelemetryValueMessage(string callsign, string comment, MetricSet metricSet)
            : this(callsign, metricSet)
        {
            Comment = comment;
        }
        
        /// <summary>
        /// Creates a new <see cref="TelemetryValueMessage"/> using another telemetry message as a template.
        /// </summary>
        /// <param name="other">The other telemetry message to use as a template.</param>
        public TelemetryValueMessage(TelemetryMessage other)
            : base(other)
        { }

        private void IncrementCounter()
        {
            SequenceNumber = Interlocked.Increment(ref _globalCounter);

            if (SequenceNumber < 1000)
                return;

            Interlocked.Exchange(ref _globalCounter, 1);
            SequenceNumber = 1;
        }

        public void SetValue(int index, float value)
        {
            if (index < 0 || index >= 5)
                throw new ArgumentOutOfRangeException(nameof(index));

            _measurements[index] = value;
            _invalidated = true;
        }

        public void SetFlag(int index, bool value)
        {
            _flags.SetFlag(index, value);
            _invalidated = true;
        }

        public float GetValue(int index)
        {
            if (index < 0 || index >= 5)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _measurements[index];
        }

        public bool GetFlag(int index) => _flags.GetFlag(index);

        /// <inheritdoc />
        public override string Compile()
        {
            if (_invalidated)
                IncrementCounter();
            _invalidated = false;
            
            var analogs = string.Join(",", MetricSet.AnalogMetrics
                                                  .Zip(_measurements, (metric, f) => new { Metric = metric, Value = f })
                                                  .Select(arg => $"{arg.Metric.TransformTo(arg.Value):000}"));

            string root =
                $"{base.Compile()}#{SequenceNumber:000},{analogs},{Convert.ToString(_flags._flags, 2).PadLeft(8, '0')}";
            return string.IsNullOrEmpty(Comment) ? root : $"{root},{Comment}";
        }
    }
}
