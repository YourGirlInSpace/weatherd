using System;

namespace weatherd.aprs
{
    public abstract class APRSMessage : ICompilable
    {
        public string TypeIdentifier { get; protected set; }

        public string SourceCallsign { get; protected set; }

        public string DestinationCallsign { get; protected set; }

        public string[] Digipeaters { get; protected set; }

        protected APRSMessage(string sourceCallsign, string destinationCallsign, string[] digipeaters)
        {
            SourceCallsign = sourceCallsign ?? throw new ArgumentNullException(nameof(sourceCallsign));
            DestinationCallsign = destinationCallsign ?? throw new ArgumentNullException(nameof(destinationCallsign));
            Digipeaters = digipeaters ?? throw new ArgumentNullException(nameof(digipeaters));
        }

        /// <summary>
        /// Compiles this APRS message into a parsable string.
        /// </summary>
        /// <returns>The APRS parsable string.</returns>
        public abstract string Compile();
    }
}
