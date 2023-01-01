using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;
using weatherd.io;

namespace weatherd.datasources.pakbus
{
    public class Table
    {
        public Table(int index, string tableName, uint tableSize, NSec interval, ushort tblSig, Field[] fields)
        {
            Index = index;
            Name = tableName;
            Size = (int) tableSize;
            Interval = interval;
            Fields = fields;
            Signature = tblSig;
        }

        public string Name { get; }
        public int Size { get; }
        public int Index { get; }
        public ushort Signature { get; }
        public NSec Interval { get; }

        public Field[] Fields { get; }
    }

    public class Field
    {
        public Field(int index, PakbusDatumType fieldType, string fieldName)
        {
            Index = index;
            Type = fieldType;
            Name = fieldName;
        }

        public int Index { get; }
        public PakbusDatumType Type { get; }
        public string Name { get; }
    }

    public class XTDTableDefinition
    {
        private readonly Dictionary<string, Table> _tables;

        public IReadOnlyList<Table> Tables => _tables.Values.ToList().AsReadOnly();

        public static XTDTableDefinition Current { get; private set; }

        private XTDTableDefinition(IEnumerable<Table> tables)
        {
            _tables = tables.ToDictionary(n => n.Name, n => n);
        }

        public Table GetTable(int index)
            => Tables[index - 1];

        public Table this[string tableName] => _tables[tableName];

        public static XTDTableDefinition Decode(byte[] data)
        {
            PakbusBinaryStream bs = new PakbusBinaryStream(data, Endianness.Big);

            List<Table> tables = new List<Table>();
            int i = 0;
            while (bs.BaseStream.Position < bs.BaseStream.Length)
            {
                tables.Add(ReadTableDefinition(bs, i+++1));
            }

            XTDTableDefinition tableDef = new XTDTableDefinition(tables.ToArray());
            Current = tableDef;

            return tableDef;
        }

        private static Table ReadTableDefinition(PakbusBinaryStream bs, int index)
        {
            long tblPtr = bs.BaseStream.Position;

            string tableName = bs.ReadStringZ();
            uint tableSize = bs.ReadUInt32();

            Debug.Assert(bs.ReadByte() == 0x0D);
            NSec time = bs.ReadUSec();

            Log.Verbose("Table '{TableName}':", tableName);
            List<Field> fields = new List<Field>();

            int i = 0;
            while (true)
            {
                Field field = ReadTableField(bs, i++);

                if (field is null)
                    break;

                fields.Add(field);
            }

            int tblLen = (int) (bs.BaseStream.Position - tblPtr);
            bs.Seek(-tblLen, SeekOrigin.Current);
            byte[] tblData = new byte[tblLen];
            int bytesRead = bs.BaseStream.Read(tblData, 0, tblLen);
            if (bytesRead != tblLen)
                throw new EndOfStreamException();

            ushort tblSig = PakbusUtilities.ComputeSignature(tblData, tblLen);

            Log.Verbose("    Signature: {Sig}", tblSig);

            return new Table(index, tableName, tableSize, time, tblSig, fields.ToArray());
        }

        private static Field ReadTableField(PakbusBinaryStream bs, int index)
        {
            PakbusDatumType fieldType = (PakbusDatumType)bs.ReadByte();

            if (fieldType == 0x0)
                return null;

            string fieldName = bs.ReadStringZ();

            string alias = bs.ReadStringZ();

            if (!string.IsNullOrEmpty(alias))
            {
                // handle aliases
            }

            bs.ReadByte(); // no idea what this signifies
            
            Log.Verbose("    Field [{Index}] '{FieldName}' ({FieldType})", index, fieldName, fieldType);

            return new Field(index, fieldType, fieldName);
        }
    }
}
