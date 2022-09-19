using System;
using weatherd.datasources.pakbus.Messages.BMP5;
using weatherd.datasources.pakbus.Messages.PakCtrl;

namespace weatherd.datasources.pakbus
{
    public abstract class PakbusMessage
    {
        public PakbusMessageType MessageType { get; protected set; }
        public byte TransactionNumber { get; protected set; }
        public byte Size { get; protected set; }

        protected PakbusMessage(PakbusMessageType msgType, byte transactionNumber)
        {
            MessageType = msgType;
            TransactionNumber = transactionNumber;
        }

        public abstract byte[] Encode();
        protected internal abstract PakbusMessage Decode(byte[] data);

        protected PakbusMessage WithData(PakbusMessageType msgType, byte transNum)
        {
            MessageType = msgType;
            TransactionNumber = transNum;
            return this;
        }

        public static PakbusMessage Decompile(PakbusProtocol protocol, byte[] bytes)
        {
            return protocol switch
            {
                PakbusProtocol.PakCtrl => PakbusPakCtrlMessage.Decompile(protocol, bytes),
                PakbusProtocol.BMP => PakbusBMP5Message.Decompile(protocol, bytes),
                PakbusProtocol.LinkState => throw new InvalidOperationException(),
                _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol out of range")
            };
        }
    }
}
