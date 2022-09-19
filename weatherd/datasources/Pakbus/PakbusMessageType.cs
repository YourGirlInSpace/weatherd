namespace weatherd.datasources.pakbus
{
    /// <summary>
    /// Represents a message typecode.
    /// </summary>
    /// <remarks>
    ///     This enum represents a message type code as a ushort.
    ///     Since message type codes can be the same across different
    ///     protocols, this enum represents a composite type code that
    ///     encodes both the protocol and message code.
    ///
    ///     Protocol    = PakbusMessageType >> 8;
    ///     MessageType = PakbusMessageType & 0xFF;
    ///
    ///     An unknown message type is encoded as 0xFFFF.
    /// </remarks>
    public enum PakbusMessageType
    {
        // PakBus Control Packets
        PakCtrl_Hello                        = 0x0009,
        PakCtrl_Bye                          = 0x000D,
        PakCtrl_HelloResponse                = 0x0089,
        PakCtrl_DevConfigGetSettingsResponse = 0x008F,
        PakCtrl_DevConfigSetSettingsResponse = 0x0090,
        PakCtrl_DevConfigControlResponse     = 0x0093,

        // BMP5 Application Packets
        BMP5_CollectDataCommand              = 0x0109,
        BMP5_CollectDataResponse             = 0x0189,
        BMP5_ClockSet                        = 0x0117,
        BMP5_ClockResponse                   = 0x0197,
        BMP5_GetProgStatResponse             = 0x0198,
        BMP5_GetValuesResponse               = 0x019A,
        BMP5_FileDownloadResponse            = 0x019C,
        BMP5_FileUploadResponse              = 0x019D,
        BMP5_FileControlResponse             = 0x019E,
        BMP5_PleaseWait                      = 0x01A1,

        // BMP5 packets for CR10X dataloggers
        BMP5_XTDGetTableDefinitions          = 0x010E,
        BMP5_XTDGetTableDefinitionsResponse  = 0x018E,
        BMP5_XTDClockSet                     = 0x0103,
        BMP5_XTDClockResponse                = 0x0183,

        Unknown                              = 0xFFFF
    }
}
