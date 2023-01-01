using System;
using weatherd.datasources.pakbus;
using weatherd.io;

namespace weatherd.tests
{
    public class RawPakbusMessage : PakbusMessage
    {
        private byte[] _data;

        /// <inheritdoc />
        public RawPakbusMessage(PakbusMessageType msgType, byte transactionNumber)
            : base(msgType, transactionNumber)
        {
        }

        /// <inheritdoc />
        public RawPakbusMessage(PakbusMessageType msgType, byte transactionNumber, byte[] data)
            : base(msgType, transactionNumber)
        {
            _data = data;
            Array.Reverse(_data);
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            PakbusBinaryStream bs = new(Endianness.Big);
            bs.Write((byte)((int)MessageType & 0xFF));
            bs.Write(TransactionNumber);

            bs.Write(_data, 0, _data.Length);

            return bs.ToArray();
        }

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            _data = data;
            return this;
        }
    }
}
