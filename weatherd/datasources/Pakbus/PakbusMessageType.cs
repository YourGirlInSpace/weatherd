namespace weatherd.datasources.Pakbus
{
    public enum PakbusMessageType
    {
        // PakBus Control Packets
        PakCtrl_Hello = 0x09,
        PakCtrl_Bye = 0x0D,
        PakCtrl_HelloResponse = 0x89,
        PakCtrl_DevConfigGetSettingsResponse = 0x8F,
        PakCtrl_DevConfigSetSettingsResponse = 0x90,
        PakCtrl_DevConfigControlResponse = 0x93,

        // BMP5 Application Packets
        BMP5_CollectDataCommand = 0x109,
        BMP5_CollectDataResponse = 0x189,
        BMP5_ClockSet = 0x117,
        BMP5_ClockResponse = 0x197,
        BMP5_GetProgStatResponse = 0x198,
        BMP5_GetValuesResponse = 0x19A,
        BMP5_FileDownloadResponse = 0x19C,
        BMP5_FileUploadResponse = 0x19D,
        BMP5_FileControlResponse = 0x19E,
        BMP5_PleaseWait = 0x1A1,

        BMP5_XTDGetTableDefinitions = 0x10E,
        BMP5_XTDGetTableDefinitionsResponse = 0x18E,
        BMP5_XTDClockSet = 0x103,
        BMP5_XTDClockResponse = 0x183,

        Unknown = 0xFFFF
    }
}
