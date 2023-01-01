using System;

namespace weatherd.aprs.telemetry
{
    using metrics;
    public class TelemetryBitSenseMessage : TelemetryMessage
    {
        internal FlagsWrapper _flags;
        public string ProjectTitle { get; }

        /// <inheritdoc />
        public TelemetryBitSenseMessage(string callsign, MetricSet metricSet)
            : base(callsign, metricSet)
        {
            TypeIdentifier = "";
            _flags = new FlagsWrapper();
        }

        /// <inheritdoc />
        public TelemetryBitSenseMessage(string callsign, string projectTitle, MetricSet metricSet)
            : base(callsign, metricSet)
        {
            TypeIdentifier = "";
            ProjectTitle = projectTitle;
            _flags = new FlagsWrapper();
        }
        
        /// <summary>
        /// Creates a new <see cref="TelemetryUnitMessage"/> using another telemetry message as a template.
        /// </summary>
        /// <param name="other">The other telemetry message to use as a template.</param>
        /// <remarks>
        ///     If you pass a <see cref="TelemetryValueMessage"/> as <paramref name="other"/>, the two messages will share
        ///     a common flags, and will update eachother if <see cref="SetFlag"/> is called on either message.
        /// </remarks>
        public TelemetryBitSenseMessage(TelemetryMessage other)
            : base(other)
        {
            _flags = new FlagsWrapper();
            if (other is TelemetryValueMessage _tvm)
                _flags = _tvm._flags;
        }

        /// <summary>
        /// Creates a new <see cref="TelemetryUnitMessage"/> using another telemetry message as a template.
        /// </summary>
        /// <param name="other">The other telemetry message to use as a template.</param>
        /// <param name="projectTitle">The name of the project associated with the telemetry station.</param>
        /// <remarks>
        ///     If you pass a <see cref="TelemetryValueMessage"/> as <paramref name="other"/>, the two messages will share
        ///     a common flags, and will update eachother if <see cref="SetFlag"/> is called on either message.
        /// </remarks>
        public TelemetryBitSenseMessage(TelemetryMessage other, string projectTitle)
            : this(other)
        {
            ProjectTitle = projectTitle;
        }

        public void SetFlag(int index, bool value) => _flags.SetFlag(index, value);

        public bool GetFlag(int index) => _flags.GetFlag(index);

        /// <inheritdoc />
        public override string Compile()
        {
            string root = $"{base.Compile()}:{SourceCallsign.PadRight(9)}:BITS.{Convert.ToString(_flags._flags, 2).PadLeft(8, '0')}";
            
            return string.IsNullOrEmpty(ProjectTitle) ? root : $"{root},{ProjectTitle}";
        }
    }
}
