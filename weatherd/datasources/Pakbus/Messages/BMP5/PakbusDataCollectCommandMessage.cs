using System.Globalization;
using Serilog;
using weatherd.io;

namespace weatherd.datasources.pakbus.Messages.BMP5
{
    public class PakbusDataCollectCommandMessage : PakbusBMP5Message
    {
        public ushort SecurityCode { get; private set; }
        public ushort TableNumber { get; private set; }
        public ushort TableSignature { get; private set; }
        public uint P1 { get; private set; }
        public uint P2 { get; private set; }
        public PakbusCollectionMode CollectMode { get; private set; }

        internal PakbusDataCollectCommandMessage()
            : base(PakbusMessageType.BMP5_CollectDataCommand, 0)
        {
        }

        public PakbusDataCollectCommandMessage(
            byte transactionNumber,
            ushort tableNum,
            ushort tableSig,
            ushort securityCode,
            PakbusCollectionMode collectMode,
            uint p1,
            uint p2)
            : base(PakbusMessageType.BMP5_CollectDataCommand, transactionNumber)
        {
            TableNumber = tableNum;
            TableSignature = tableSig;
            SecurityCode = securityCode;
            CollectMode = collectMode;
            P1 = p1;
            P2 = p2;
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            var bs = new BinaryStream(Endianness.Big);
            bs.Write((byte)((int)MessageType & 0xFF));
            bs.Write(TransactionNumber);

            bs.Write(SecurityCode);
            bs.Write((byte)CollectMode);
            bs.Write(TableNumber);
            bs.Write(TableSignature);

            switch (CollectMode)
            {
                case PakbusCollectionMode.GetDataFromRecord:
                case PakbusCollectionMode.GetLastRecord:
                    bs.Write(P1);
                    break;
                case PakbusCollectionMode.GetDataRange:
                case PakbusCollectionMode.GetRecordsBetweenTimes:
                case PakbusCollectionMode.GetPartialRecord:
                    bs.Write(P1);
                    bs.Write(P2);
                    break;
            }

            bs.Write((byte)0);

            return bs.ToArray();
        }

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            var bs = new BinaryStream(data, Endianness.Big);
            bs.Skip(2);

            ushort securityCode = bs.ReadUInt16();

            var collectMode = (PakbusCollectionMode)bs.ReadByte();

            int tblNum = bs.ReadUInt16();
            int tblSig = bs.ReadUInt16();

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (collectMode)
            {
                case PakbusCollectionMode.GetDataFromRecord:
                case PakbusCollectionMode.GetLastRecord:
                    P1 = bs.ReadUInt32();
                    break;
                case PakbusCollectionMode.GetDataRange:
                case PakbusCollectionMode.GetRecordsBetweenTimes:
                case PakbusCollectionMode.GetPartialRecord:
                    P1 = bs.ReadUInt32();
                    P2 = bs.ReadUInt32();
                    break;
            }

            switch (collectMode)
            {
                case PakbusCollectionMode.GetDataRange:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect records between {P1} and {P2} inclusive on table {TableNumber}",
                        P1.ToString(CultureInfo.CurrentCulture),
                        P2.ToString(CultureInfo.CurrentCulture),
                        tblNum.ToString(CultureInfo.CurrentCulture));
                    break;
                case PakbusCollectionMode.GetLastRecord:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect last {P1} records on table {TableNumber}",
                        P1.ToString(CultureInfo.CurrentCulture),
                        tblNum.ToString(CultureInfo.CurrentCulture));
                    break;
                case PakbusCollectionMode.GetPartialRecord:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect a partial record at record number {P1} and byte offset {P2} on table {TableNumber}",
                        P1.ToString(CultureInfo.CurrentCulture),
                        P2.ToString(CultureInfo.CurrentCulture),
                        tblNum.ToString(CultureInfo.CurrentCulture));
                    break;
                case PakbusCollectionMode.GetAllData:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect all data stored on the logger on table {TableNumber}",
                        tblNum.ToString(CultureInfo.CurrentCulture));
                    break;
                case PakbusCollectionMode.GetDataFromRecord:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect from {P1} to the latest record on table {TableNumber}",
                        P1.ToString(CultureInfo.CurrentCulture),
                        tblNum.ToString(CultureInfo.CurrentCulture));
                    break;
                case PakbusCollectionMode.GetRecordsBetweenTimes:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect the time swath described by {P1} and {P2} on table {TableNumber}",
                        P1.ToString(CultureInfo.CurrentCulture),
                        P2.ToString(CultureInfo.CurrentCulture),
                        tblNum.ToString(CultureInfo.CurrentCulture));
                    break;
                case PakbusCollectionMode.InquireRecordInfo:
                case PakbusCollectionMode.StoreData:
                default:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect all data stored on the logger on table {TableNumber}",
                        tblNum.ToString(CultureInfo.CurrentCulture));
                    break;
            }

            TableNumber = (ushort)tblNum;
            TableSignature = (ushort)tblSig;
            SecurityCode = securityCode;
            CollectMode = collectMode;
            return this;
        }
    }
}
