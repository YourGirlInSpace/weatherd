using weatherd.io;

namespace weatherd.datasources.pakbus.Messages.BMP5
{
    public class PakbusXTDGetTableDefinitionsCommand : PakbusBMP5Message
    {
        public int SecurityCode { get; private set; }
        public int FragmentNumber { get; private set; }

        public PakbusXTDGetTableDefinitionsCommand(byte transactionNumber, int securityCode, int fragmentNumber)
            : base(PakbusMessageType.BMP5_XTDGetTableDefinitions, transactionNumber)
        {
            SecurityCode = securityCode;
            FragmentNumber = fragmentNumber;
        }

        public PakbusXTDGetTableDefinitionsCommand()
            : base(PakbusMessageType.BMP5_XTDGetTableDefinitions, 0)
        {
        }

        /// <inheritdoc />
        protected PakbusXTDGetTableDefinitionsCommand(PakbusMessageType msgType, byte transactionNumber) : base(
            msgType, transactionNumber)
        {
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            var bs = new BinaryStream(Endianness.Big);
            bs.Write((byte)((int)MessageType & 0xFF));
            bs.Write(TransactionNumber);

            bs.Write((ushort)SecurityCode);
            bs.Write((ushort)FragmentNumber);

            return bs.ToArray();
        }

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            var bs = new BinaryStream(data, Endianness.Big);

            bs.Skip(2);

            int securityCode = bs.ReadUInt16();
            int fragmentNum = bs.ReadUInt16();

            SecurityCode = securityCode;
            FragmentNumber = fragmentNum;

            return this;
        }
    }
}
