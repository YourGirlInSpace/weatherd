using System;

namespace weatherd.datasources.pakbus.Messages.PakCtrl
{
    public class PakbusPakCtrlUnknownMessage : PakbusPakCtrlMessage
    {
        /// <inheritdoc />
        public PakbusPakCtrlUnknownMessage(PakbusMessageType msgType, byte transactionNumber) : base(
            msgType, transactionNumber)
        {
        }

        /// <inheritdoc />
        public override byte[] Encode() => throw new NotImplementedException();

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data) => this;
    }
}
