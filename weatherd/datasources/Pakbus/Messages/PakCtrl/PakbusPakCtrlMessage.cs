using System;
using Serilog;

namespace weatherd.datasources.Pakbus.Messages.PakCtrl
{
    public abstract class PakbusPakCtrlMessage : PakbusMessage
    {
        /// <inheritdoc />
        protected PakbusPakCtrlMessage(PakbusMessageType msgType, byte transactionNumber) : base(msgType, transactionNumber)
        {
        }

        public new static PakbusMessage Decompile(PakbusProtocol protocol, byte[] bytes)
        {
            int msgTypeRaw = bytes[0];
            msgTypeRaw |= (byte)protocol << 8;
            PakbusMessageType msgType = (PakbusMessageType)msgTypeRaw;

            byte transNum = bytes[1];

            switch (msgType)
            {
                case PakbusMessageType.PakCtrl_Hello:
                    return new PakbusHelloMessage().WithData(msgType, transNum).Decode(bytes);
                case PakbusMessageType.PakCtrl_Bye:
                    return null;
                case PakbusMessageType.PakCtrl_HelloResponse:
                    return new PakbusHelloResponseMessage().WithData(msgType, transNum).Decode(bytes);
                case PakbusMessageType.PakCtrl_DevConfigControlResponse:
                case PakbusMessageType.PakCtrl_DevConfigGetSettingsResponse:
                case PakbusMessageType.PakCtrl_DevConfigSetSettingsResponse:
                    throw new NotImplementedException();
                default:
                    Log.Warning("[Pakbus] Unknown {protocol} message type: {msgType:X}", protocol, msgType);
                    return null;
            }
        }
    }
}
