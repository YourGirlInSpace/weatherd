using Serilog;
using weatherd.io;

namespace weatherd.datasources.pakbus.Messages.PakCtrl
{
    public class PakbusHelloMessage : PakbusPakCtrlMessage
    {
        public byte IsRouter { get; set; }
        public byte HopMetric { get; set; }

        /// <inheritdoc />
        public PakbusHelloMessage(byte transactionNumber) : base(PakbusMessageType.PakCtrl_Hello, transactionNumber)
        {
        }

        public PakbusHelloMessage() : base(PakbusMessageType.PakCtrl_Hello, 0)
        {
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            var bs = new BinaryStream(Endianness.Big);
            bs.Write((byte)((int)MessageType & 0xFF));
            bs.Write(TransactionNumber);

            bs.Write(IsRouter);
            bs.Write(HopMetric);
            bs.Write((ushort)0xFFFF);

            return bs.ToArray();
        }

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            var bs = new BinaryStream(data, Endianness.Big);
            bs.Skip(2);

            IsRouter = bs.ReadByte();
            HopMetric = bs.ReadByte();

            Log.Verbose("[Pakbus] Hello?  IsRouter={IsRouter:X}, HopMetric={HopMetric:X}", IsRouter, HopMetric);

            return this;
        }
    }
}
