using System;
using weatherd.datasources.Pakbus.Messages.BMP5;
using weatherd.datasources.Pakbus.Messages.PakCtrl;

namespace weatherd.datasources.Pakbus
{
    public abstract class PakbusMessage
    {
        public PakbusMessageType MessageType { get; protected set; }
        public byte TransactionNumber { get; protected set; }
        public byte Size { get; protected set; }

        public abstract byte[] Encode();
        protected internal abstract PakbusMessage Decode(byte[] data);

        protected PakbusMessage(PakbusMessageType msgType, byte transactionNumber)
        {
            MessageType = msgType;
            TransactionNumber = transactionNumber;
        }

        protected PakbusMessage WithData(PakbusMessageType msgType, byte transNum)
        {
            MessageType = msgType;
            TransactionNumber = transNum;
            return this;
        }

        public static PakbusMessage Decompile(PakbusProtocol protocol, byte[] bytes)
        {
            switch (protocol)
            {
                case PakbusProtocol.PakCtrl:
                    return PakbusPakCtrlMessage.Decompile(protocol, bytes);
                case PakbusProtocol.BMP:
                    return PakbusBMP5Message.Decompile(protocol, bytes);
                default:
                    throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol out of range");
            }
        }
    }
}
