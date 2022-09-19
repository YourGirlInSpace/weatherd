namespace weatherd.datasources.pakbus.Messages.BMP5
{
    public enum PakbusXTDResponseCode
    {
        ClockNotChanged = 0,
        PermissionDenied = 1,
        InsufficientResources = 2,
        Unknown = 3,
        ClockChanged = 4,
        UncompilableProgram = 5,
        InvalidTableDefinitionSignature = 7,
        InvalidProgramCode = 8,
        InvalidFragmentNumber = 9,
        UncompilableProgramShortDLDPROM = 11,
        InvalidFileName = 13,
        FileNotCurrentlyAccessable = 14,
        OptionDoesNotApply = 15,
        InvalidTableOrFieldName = 16,
        UnsupportedDataTypeConversion = 17,
        MemoryBoundsViolation = 18,
        UnsupportedFileCommand = 19
    }
}
