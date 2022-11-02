using System;
using Serilog;
using weatherd.io;

namespace weatherd.datasources.pakbus.Messages.PakCtrl
{
    public class PakbusHelloResponseMessage : PakbusPakCtrlMessage
    {
        public byte IsRouter { get; private set; }
        public byte HopMetric { get; private set; }

        

        public PakbusHelloResponseMessage(byte transactionNumber, byte isRouter, byte hopMetric)
            : base(PakbusMessageType.PakCtrl_HelloResponse, transactionNumber)
        {
            IsRouter = isRouter;
            HopMetric = hopMetric;
        }

        /// <inheritdoc />
        public PakbusHelloResponseMessage(PakbusMessageType msgType, byte transactionNumber) : base(
            msgType, transactionNumber)
        {
        }

        public PakbusHelloResponseMessage()
            : base(PakbusMessageType.PakCtrl_HelloResponse, 0)
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

            return bs.ToArray();
        }

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            IsRouter = data[0];
            HopMetric = data[1];

            Log.Verbose("[Pakbus] Hello!  IsRouter={isRouter:X}, HopMetric={hopMetric:X}", IsRouter, HopMetric);

            return this;
        }
    }
}
