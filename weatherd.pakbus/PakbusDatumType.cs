namespace weatherd.datasources.pakbus
{
    public enum PakbusDatumType : byte
    {
        Byte    = 1,
        UInt2   = 2,
        UInt4   = 3,
        Int1    = 4,
        Int2    = 5,
        Int4    = 6,
        FP2     = 7,
        FP3     = 15,
        FP4     = 8,
        IEEE4B  = 9,
        IEEE8B  = 18,
        Bool8   = 17,
        Bool    = 10,
        Bool2   = 27,
        Bool4   = 28,
        Sec     = 12,
        USec    = 13,
        NSec    = 14,
        ASCII   = 11,
        ASCIIZ  = 16,
        Short   = 19,
        Long    = 20,
        UShort  = 21,
        ULong   = 22,
        IEEE4L  = 24,
        IEEE8L  = 25,
        SecNano = 23
    }
}
