namespace weatherd.datasources.Pakbus.Messages.BMP5
{
    public class PakbusBMP5UnknownMessage : PakbusBMP5Message
    {
        /// <inheritdoc />
        public PakbusBMP5UnknownMessage(PakbusMessageType msgType, byte transactionNumber) : base(msgType, transactionNumber)
        {
        }

        public PakbusBMP5UnknownMessage()
            : base(PakbusMessageType.Unknown, 0)
        {
        }

        /// <inheritdoc />
        public override byte[] Encode() => throw new System.NotImplementedException();

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data) => this;
    }
}
