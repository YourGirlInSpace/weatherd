using System;
using System.IO;
using Serilog;
using weatherd.io;

namespace weatherd.datasources.pakbus.Messages.BMP5
{
    public class PakbusDataCollectResponseMessage : PakbusBMP5Message
    {
        public PakbusResult Results { get; } = new();

        public object this[string fieldName] => Results.Get<object>(fieldName);

        /// <inheritdoc />
        public PakbusDataCollectResponseMessage(PakbusMessageType msgType, byte transactionNumber) : base(
            msgType, transactionNumber)
        {
        }

        public PakbusDataCollectResponseMessage()
            : base(PakbusMessageType.BMP5_CollectDataResponse, 0)
        {
        }

        /// <inheritdoc />
        public override byte[] Encode() => throw new NotImplementedException();

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            var bs = new PakbusBinaryStream(data, Endianness.Big);
            bs.Skip(2);

            if (data.Length < 12)
            {
                Log.Information("[Pakbus] Collection error: Invalid response - data packet smaller than 12 bytes");
                return null;
            }

            byte responseCode = bs.ReadByte();
            switch (responseCode)
            {
                case 0x01:
                    Log.Information("[Pakbus] Collection error: permission denied");
                    return null;
                case 0x02:
                    Log.Information("[Pakbus] Collection error: insufficient resources");
                    return null;
                case 0x07:
                    Log.Information("[Pakbus] Collection error: invalid TDF");
                    return null;
                case 0x00:
                    // Success, ignore
                    break;
                default:
                    Log.Information("[Pakbus] Collection error: error code {ErrorCode:X}", responseCode);
                    return null;
            }

            ushort tableNum = bs.ReadUInt16();
            uint startRecord = bs.ReadUInt32();

            bool isOffset = bs.ReadByte() >> 7 > 0;

            if (isOffset)
            {
                // Ignore
                //uint byteOffset = bs.ReadUInt32();
            } else
            {
                bs.Seek(-1, SeekOrigin.Current);

                // Skip the time field
                NSec nsec = bs.ReadUSec();

                Results.Add("RECTIME", nsec.ToUnixTimestamp());
                Results.Add("RECNO", (int) startRecord);

                Table table = XTDTableDefinition.Current.GetTable(tableNum);

                foreach (Field field in table.Fields)
                    Results.Add(field.Name, bs.Read(field));
            }
            
            return this;
        }
    }
}
