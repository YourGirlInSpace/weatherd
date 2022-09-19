using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using weatherd.io;

namespace weatherd.datasources.Pakbus.Messages.BMP5
{
    public class PakbusXTDGetTableDefinitionsResponse : PakbusBMP5Message
    {
        public byte ResponseCode { get; private set; }
        public bool MoreFragments { get; private set; }
        public byte FragmentNumber { get; private set; }

        public byte[] Fragment { get; private set; }

        public PakbusXTDGetTableDefinitionsResponse()
            : base(PakbusMessageType.BMP5_XTDGetTableDefinitionsResponse, 0)
        { }
        /// <inheritdoc />
        protected PakbusXTDGetTableDefinitionsResponse(PakbusMessageType msgType, byte transactionNumber) : base(msgType, transactionNumber)
        {
        }

        /// <inheritdoc />
        public override byte[] Encode() => throw new System.NotImplementedException();

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            BinaryStream bs = new BinaryStream(data, Endianness.Big);

            bs.Skip(2);

            byte responseCode = bs.ReadByte();
            bool moreFragments = bs.ReadBoolean();
            byte fragmentNumber = bs.ReadByte();

            byte[] dataBytes = new byte[bs.BaseStream.Length - bs.BaseStream.Position];
            int bytesRead = bs.BaseStream.Read(dataBytes, 0, dataBytes.Length);

            if (bytesRead != dataBytes.Length)
                throw new EndOfStreamException();

            ResponseCode = responseCode;
            MoreFragments = moreFragments;
            FragmentNumber = fragmentNumber;
            Fragment = dataBytes;

            return this;
        }
    }
}
