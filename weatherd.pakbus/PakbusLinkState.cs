namespace weatherd.datasources.pakbus
{
    public enum PakbusLinkState
    {
        Ring = 0x9,
        Ready = 0xA,
        Finished = 0xB,
        Pause = 0xC,
        Broadcast = 0x8
    }
}
