using System;
using System.IO;
using System.Text;
using Serilog;
using weatherd.io;

namespace weatherd.datasources.Pakbus.Messages.BMP5
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
            : base(PakbusMessageType.BMP5_CollectDataCommand, 0) { }

        public PakbusDataCollectCommandMessage(byte transactionNumber, ushort tableNum, ushort tableSig, ushort securityCode, PakbusCollectionMode collectMode, uint p1, uint p2)
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
            BinaryStream bs = new BinaryStream(Endianness.Big);
            bs.Write((byte) ((int)MessageType & 0xFF));
            bs.Write(TransactionNumber);

            bs.Write(SecurityCode);
            bs.Write((byte) CollectMode);
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

            bs.Write((byte) 0);

            return bs.ToArray();
        }

        /// <inheritdoc />
        protected internal override PakbusMessage Decode(byte[] data)
        {
            BinaryStream bs = new BinaryStream(data, Endianness.Big);
            bs.Skip(2);

            ushort securityCode = bs.ReadUInt16();

            PakbusCollectionMode collectMode = (PakbusCollectionMode) bs.ReadByte();
            int msgLen = data.Length - 3;

            int tblNum = bs.ReadUInt16();
            int tblSig = bs.ReadUInt16();
            
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
                        "[Pakbus] Collect command:  Collect records between {p1} and {p2} on table {tblNum}.  Include {p1} but exclude {p2}",
                        P1, P2, P1, P2, tblNum);
                    break;
                case PakbusCollectionMode.GetLastRecord:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect last {p1} records on table {tblNum}",
                        P1, tblNum);
                    break;
                case PakbusCollectionMode.GetPartialRecord:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect a partial record at record number {p1} and byte offset {p2} on table {tblNum}",
                        P1, P2, tblNum);
                    break;
                case PakbusCollectionMode.GetAllData:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect all data stored on the logger on table {tblNum}", tblNum);
                    break;
                case PakbusCollectionMode.GetDataFromRecord:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect from {p1} to the latest record on table {tblNum}", P1, tblNum);
                    break;
                case PakbusCollectionMode.GetRecordsBetweenTimes:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect the time swath described by {p1} and {p2} on table {tblNum}", P1, P2, tblNum);
                    break;
                default:
                    Log.Verbose(
                        "[Pakbus] Collect command:  Collect all data stored on the logger on table {tblNum}", tblNum);
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
