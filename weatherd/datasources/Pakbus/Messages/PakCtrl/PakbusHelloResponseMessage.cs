using Serilog;

namespace weatherd.datasources.Pakbus.Messages.PakCtrl
{
    public class PakbusHelloResponseMessage : PakbusPakCtrlMessage
    {
        /// <inheritdoc />
        public PakbusHelloResponseMessage(PakbusMessageType msgType, byte transactionNumber) : base(msgType, transactionNumber)
        {
        }

        public PakbusHelloResponseMessage()
            : base(PakbusMessageType.PakCtrl_HelloResponse, 0)
        { }

        public byte IsRouter { get; private set; }
        public byte HopMetric { get; private set; }

        /// <inheritdoc />
        public override byte[] Encode() => throw new System.NotImplementedException();

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
