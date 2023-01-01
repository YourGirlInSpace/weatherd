namespace weatherd.aprs
{
    public abstract class APRSISMessage : APRSMessage
    {

        /// <inheritdoc />
        protected APRSISMessage(string sourceCallsign, string destinationCallsign)
            : base(sourceCallsign, destinationCallsign, new []{ "TCPIP*" })
        {
        }

        /// <inheritdoc />
        public override string Compile() => $"{SourceCallsign}>{DestinationCallsign},{string.Join(",", Digipeaters)}:";
    }
}
