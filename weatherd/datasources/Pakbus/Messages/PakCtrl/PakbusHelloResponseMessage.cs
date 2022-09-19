using System;
using Serilog;

namespace weatherd.datasources.pakbus.Messages.PakCtrl
{
    public class PakbusHelloResponseMessage : PakbusPakCtrlMessage
    {
        public byte IsRouter { get; private set; }
        public byte HopMetric { get; private set; }

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
        public override byte[] Encode() => throw new NotImplementedException();

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
