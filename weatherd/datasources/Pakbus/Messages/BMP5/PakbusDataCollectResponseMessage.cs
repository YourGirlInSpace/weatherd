using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using weatherd.io;

namespace weatherd.datasources.pakbus.Messages.BMP5
{
    public class PakbusDataCollectResponseMessage : PakbusBMP5Message
    {
        public object this[string fieldName] => _results[fieldName];

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
                    Log.Information("[Pakbus] Collection error: error code {errCode:X}", responseCode);
                    return null;
            }

            ushort tableNum = bs.ReadUInt16();

            // Skip next two bytes
            uint startRecord = bs.ReadUInt32();

            bool isOffset = bs.ReadByte() >> 7 > 0;

            if (isOffset)
            {
                uint byteOffset = bs.ReadUInt32();
            } else
            {
                bs.Seek(-1, SeekOrigin.Current);
                int numRecords = bs.ReadUInt16();

                // Skip the time field
                NSec nsec = bs.ReadUSec();

                //Log.Debug("Time Sec={timeSec} NSec={nSec}, Calc Time={calcTime}", nsec.Seconds + 631152000, nsec.Nanoseconds, nsec.ToTime());

                _results["RECTIME"] = nsec.ToUnixTimestamp();
                _results["RECNO"] = (int)startRecord;

                Table table = XTDTableDefinition.Current.GetTable(tableNum);

                foreach (Field field in table.Fields)
                    _results[field.Name] = bs.Read(field);
            }

            //Log.Information("Table={table}\n               Record={record}\n               Flags={flags:X}\n               Ports={ports:X}\n               BattV={battV:F2}V\n               Sig={progSig:F2}\n               PTemp_C={ptempC:F2}°C\n               T107_C={t107c:F2}°C", tableNum, recordNum, flags, ports, battV, progSig, ptemp_c, t107_c);

            return this;
        }

        private readonly Dictionary<string, object> _results = new();
    }
}
