using System;

namespace weatherd.datasources.Pakbus
{
    [Flags]
    public enum PakbusCollectionMode
    {
        GetLastRecord = 0x05,
        GetDataRange = 0x06,
        GetPartialRecord = 0x08,
        GetAllData = 0x03,
        GetDataFromRecord = 0x04,
        GetRecordsBetweenTimes = 0x07,
        InquireRecordInfo = 0x10,
        StoreData = 0x20
    }
}
