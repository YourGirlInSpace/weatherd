using System;
using weatherd.io;

namespace weatherd.datasources.pakbus.Messages.BMP5
{
    public class PakbusXTDClockCommand : PakbusBMP5Message
    {
        public ushort SecurityCode { get; private set; }
        public ushort MaxDiff { get; private set; }
        public NSec Time { get; private set; }

        public PakbusXTDClockCommand()
            : base(PakbusMessageType.BMP5_XTDClockSet, 0)
        {
        }

        /// <inheritdoc />
        public PakbusXTDClockCommand(PakbusMessageType msgType, byte transactionNumber)
            : base(msgType, transactionNumber)
        {
        }

        public PakbusXTDClockCommand(byte transactionID, ushort securityCode, DateTime time)
            : base(PakbusMessageType.BMP5_XTDClockSet, transactionID)
        {
            SecurityCode = securityCode;
            MaxDiff = 1;
            Time = NSec.FromTime(time);
        }

        public PakbusXTDClockCommand(byte transactionID, ushort securityCode)
            : base(PakbusMessageType.BMP5_XTDClockSet, transactionID)
        {
            SecurityCode = securityCode;
            MaxDiff = 0;
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            var bs = new PakbusBinaryStream(Endianness.Big);
            bs.Write((byte)((int)MessageType & 0xFF));
            bs.Write(TransactionNumber);

            bs.Write(SecurityCode);
            bs.Write(MaxDiff);
            if (MaxDiff == 0x0)
                return bs.ToArray();

            bs.WriteUSec(Time);
            bs.Write((byte)0x0);

            return bs.ToArray();
        }

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            var bs = new PakbusBinaryStream(data, Endianness.Big);

            bs.Skip(2);

            ushort secCode = bs.ReadUInt16();
            ushort maxDiff = bs.ReadUInt16();

            NSec nsec = NSec.Zero;
            if (maxDiff != 0x0)
                nsec = bs.ReadUSec();

            SecurityCode = secCode;
            MaxDiff = maxDiff;
            Time = nsec;
            return this;
        }
    }
}
