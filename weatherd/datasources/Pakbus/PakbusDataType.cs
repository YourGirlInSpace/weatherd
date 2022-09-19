namespace weatherd.datasources.Pakbus
{
    public enum PakbusDataType : byte
    {
        [PakbusDataCode(EncodeAs = typeof(byte), Size = 1)]
        Byte = 1,

        [PakbusDataCode(EncodeAs = typeof(ushort), BigEndian = true, Size = 2)]
        UInt2 = 2,

        [PakbusDataCode(EncodeAs = typeof(uint), BigEndian = true, Size = 4)]
        UInt4 = 3,

        [PakbusDataCode(EncodeAs = typeof(byte), Size = 1)]
        Int1 = 4,

        [PakbusDataCode(EncodeAs = typeof(short), BigEndian = true, Size = 2)]
        Int2 = 5,

        [PakbusDataCode(EncodeAs = typeof(int), BigEndian = true, Size = 4)]
        Int4 = 6,

        [PakbusDataCode(EncodeAs = typeof(ushort), BigEndian = true, Size = 2)]
        FP2 = 7,

        [PakbusDataCode(EncodeAs = typeof(char), Quantity = 3, Size = 3)]
        FP3 = 15,

        [PakbusDataCode(EncodeAs = typeof(char), Quantity = 4, Size = 4)]
        FP4 = 8,

        [PakbusDataCode(EncodeAs = typeof(float), BigEndian = true, Size = 4)]
        IEEE4B = 9,

        [PakbusDataCode(EncodeAs = typeof(double), BigEndian = true, Size = 8)]
        IEEE8B = 18,

        [PakbusDataCode(EncodeAs = typeof(bool), Size = 1)]
        Bool8 = 17,

        [PakbusDataCode(EncodeAs = typeof(bool), Size = 1)]
        Bool = 10,

        [PakbusDataCode(EncodeAs = typeof(ushort), BigEndian = true, Size = 2)]
        Bool2 = 27,

        [PakbusDataCode(EncodeAs = typeof(uint), BigEndian = true, Size = 4)]
        Bool4 = 28,

        [PakbusDataCode(EncodeAs = typeof(int), BigEndian = true, Size = 4)]
        Sec = 12,

        [PakbusDataCode(EncodeAs = typeof(char), Quantity = 6, Size = 6)]
        USec = 13,

        [PakbusDataCode(EncodeAs = typeof(char), BigEndian = true, Quantity = 2, Size = 8)]
        NSec = 14,

        [PakbusDataCode(EncodeAs = typeof(string), Size = 0)]
        ASCII = 11,

        [PakbusDataCode(EncodeAs = typeof(string), Size = 0)]
        ASCIIZ = 16,

        [PakbusDataCode(EncodeAs = typeof(short), BigEndian = false, Size = 2)]
        Short = 19,

        [PakbusDataCode(EncodeAs = typeof(int), BigEndian = false, Size = 4)]
        Int = 20,

        [PakbusDataCode(EncodeAs = typeof(ushort), BigEndian = false, Size = 2)]
        UShort = 21,

        [PakbusDataCode(EncodeAs = typeof(ulong), BigEndian = false, Size = 4)]
        UInt = 22,

        [PakbusDataCode(EncodeAs = typeof(float), BigEndian = false, Size = 4)]
        IEEE4L = 24,

        [PakbusDataCode(EncodeAs = typeof(double), BigEndian = false, Size = 8)]
        IEEE8L = 25,

        [PakbusDataCode(EncodeAs = typeof(int), BigEndian = false, Quantity = 2, Size = 8)]
        SecNano = 23
    }
}
