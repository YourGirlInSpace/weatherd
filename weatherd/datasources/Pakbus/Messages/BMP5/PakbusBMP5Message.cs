using Serilog;

namespace weatherd.datasources.pakbus.Messages.BMP5
{
    public abstract class PakbusBMP5Message : PakbusMessage
    {
        protected PakbusBMP5Message(PakbusMessageType msgType, byte transactionNumber)
            : base(msgType, transactionNumber)
        {
        }

        public new static PakbusMessage Decompile(PakbusProtocol protocol, byte[] bytes)
        {
            int msgTypeRaw = bytes[0];
            msgTypeRaw |= (byte)protocol << 8;
            var msgType = (PakbusMessageType)msgTypeRaw;

            byte transNum = bytes[1];

            switch (msgType)
            {
                case PakbusMessageType.BMP5_CollectDataCommand:
                    return new PakbusDataCollectCommandMessage().WithData(msgType, transNum).Decode(bytes);
                case PakbusMessageType.BMP5_CollectDataResponse:
                    return new PakbusDataCollectResponseMessage().WithData(msgType, transNum).Decode(bytes);
                case PakbusMessageType.BMP5_XTDGetTableDefinitions:
                    return new PakbusXTDGetTableDefinitionsCommand().WithData(msgType, transNum).Decode(bytes);
                case PakbusMessageType.BMP5_XTDGetTableDefinitionsResponse:
                    return new PakbusXTDGetTableDefinitionsResponse().WithData(msgType, transNum).Decode(bytes);
                case PakbusMessageType.BMP5_XTDClockSet:
                    return new PakbusXTDClockCommand().WithData(msgType, transNum).Decode(bytes);
                case PakbusMessageType.BMP5_XTDClockResponse:
                    return new PakbusXTDClockResponse().WithData(msgType, transNum).Decode(bytes);
                case PakbusMessageType.BMP5_ClockResponse:
                case PakbusMessageType.BMP5_GetProgStatResponse:
                case PakbusMessageType.BMP5_GetValuesResponse:
                case PakbusMessageType.BMP5_FileDownloadResponse:
                case PakbusMessageType.BMP5_FileUploadResponse:
                case PakbusMessageType.BMP5_FileControlResponse:
                //case PakbusMessageType.BMP5_PleaseWait:
                //    return new PakbusBMP5UnknownMessage().WithData(msgType, transNum).Decode(bytes);
                default:
                    Log.Warning("[Pakbus] Unknown {Protocol} message type: {MessageType:X}", protocol, msgType);
                    return null;
            }
        }
    }
}
