using weatherd.io;

namespace weatherd.datasources.Pakbus.Messages.BMP5
{
    public class PakbusXTDClockResponse : PakbusBMP5Message
    {
        public PakbusXTDResponseCode ResponseCode { get; set; }
        public NSec Time { get; set; }

        /// <inheritdoc />
        public PakbusXTDClockResponse(PakbusMessageType msgType, byte transactionNumber) : base(msgType, transactionNumber)
        { }

        public PakbusXTDClockResponse()
            : base(PakbusMessageType.BMP5_XTDClockResponse, 0)
        { }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            PakbusBinaryStream bs = new PakbusBinaryStream(Endianness.Big);
            bs.Write((byte) ((int)MessageType & 0xFF));
            bs.Write(TransactionNumber);

            bs.Write((byte)ResponseCode);
            bs.WriteUSec(Time);
            bs.Write((byte) 0x0);
            
            return bs.ToArray();

        }

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            PakbusBinaryStream bs = new PakbusBinaryStream(data, Endianness.Big);

            bs.Skip(2);

            byte respCode = bs.ReadByte();
            NSec time = bs.ReadUSec();

            ResponseCode = (PakbusXTDResponseCode) respCode;
            Time = time;

            return this;
        }
    }
}
