using System;
using Serilog;

namespace weatherd.datasources.Pakbus
{
    public class PakbusHeader
    {
        public PakbusHeaderType Type { get; }
        public uint DestinationNodeID { get; }

        public uint SourceNodeID { get; }

        public uint DestinationPhysicalAddress { get; }

        public uint SourcePhysicalAddress { get; }

        public PakbusProtocol Protocol { get; }

        public byte ExpectModeCode { get; } = 0x2;

        public PakbusLinkState LinkState { get; } = PakbusLinkState.Ready;

        public PakbusPriority Priority { get; } = PakbusPriority.Normal;
        public byte HopCount { get; }
        
        public PakbusHeader(PakbusHeaderType type,
            uint dstNodeId,
            uint srcNodeId,
            PakbusProtocol protocol)
        {
            Type = type;
            DestinationNodeID = DestinationPhysicalAddress = dstNodeId;
            SourceNodeID = SourcePhysicalAddress = srcNodeId;
            Protocol = protocol;
        }
        
        public PakbusHeader(PakbusHeaderType type,
            uint dstNodeId,
            uint srcNodeId,
            PakbusProtocol protocol,
            byte expMoreCode,
            PakbusLinkState linkState,
            PakbusPriority priority,
            byte hopCnt)
        {
            Type = type;
            DestinationNodeID = DestinationPhysicalAddress = dstNodeId;
            SourceNodeID = SourcePhysicalAddress = srcNodeId;
            Protocol = protocol;
            ExpectModeCode = expMoreCode;
            LinkState = linkState;
            Priority = priority;
            HopCount = hopCnt;
        }
        
        public PakbusHeader(PakbusHeaderType type,
            uint dstNodeId,
            uint srcNodeId,
            uint dstPhysAddr,
            uint srcPhysAddr,
            PakbusProtocol protocol)
        {
            Type = type;
            DestinationNodeID = dstNodeId;
            SourceNodeID = srcNodeId;
            DestinationPhysicalAddress = dstPhysAddr;
            SourcePhysicalAddress = srcPhysAddr;
            Protocol = protocol;
        }
        
        public PakbusHeader(PakbusHeaderType type,
            uint dstNodeId,
            uint srcNodeId,
            uint dstPhysAddr,
            uint srcPhysAddr,
            PakbusProtocol protocol,
            byte expMoreCode,
            PakbusLinkState linkState,
            PakbusPriority priority,
            byte hopCnt)
        {
            Type = type;
            DestinationNodeID = dstNodeId;
            SourceNodeID = srcNodeId;
            DestinationPhysicalAddress = dstPhysAddr;
            SourcePhysicalAddress = srcPhysAddr;
            Protocol = protocol;
            ExpectModeCode = expMoreCode;
            LinkState = linkState;
            Priority = priority;
            HopCount = hopCnt;
        }

        public byte[] Encode()
        {
            switch (Type)
            {
                case PakbusHeaderType.Normal:
                {
                    ulong uSignature = (((byte)LinkState & 0xFul) << 60)
                                       | ((DestinationPhysicalAddress & 0xFFFul) << 48)
                                       | ((ExpectModeCode & 0x3ul) << 46)
                                       | (((byte)Priority & 0x3ul) << 44)
                                       | ((SourcePhysicalAddress & 0xFFFul) << 32)
                                       | (((byte)Protocol & 0xFul) << 28)
                                       | ((DestinationNodeID & 0xFFFul) << 16)
                                       | ((HopCount & 0xFul) << 12)
                                       | (SourceNodeID & 0xFFF);

                    byte[] pk = BitConverter.GetBytes(uSignature);
                    Array.Reverse(pk);
                    return pk;
                }
                case PakbusHeaderType.CompressedLinkState:
                {
                    byte[] buffer = new byte[4];
                    buffer[0] |= (byte) ((byte)LinkState << 4);
                    buffer[0] |= (byte)(DestinationPhysicalAddress >> 8);
                        
                    buffer[1] = (byte)(DestinationPhysicalAddress & 0xFF);
                    buffer[2] = (byte)((uint)(ExpectModeCode | (byte)Priority) | (SourcePhysicalAddress >> 8));
                    buffer[3] = (byte)(SourcePhysicalAddress & 0xFF);

                    return buffer;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        public static PakbusHeader Decompile(ulong data)
        {
            byte linkState = (byte)((data >> 60) & 0xF);
            uint dstPhyAddr = (uint)((data >> 48) & 0xFFF);
            byte expMoreCode = (byte)((data >> 46) & 0x3);
            byte priority = (byte)((data >> 44) & 0x3);
            uint srcPhyAddr = (uint)((data >> 32) & 0xFFF);
            PakbusProtocol hiProtoCode = (PakbusProtocol)((data >> 28) & 0xF);
            uint dstNodeId = (uint)((data >> 16) & 0xFFF);
            byte hopCnt = (byte)((data >> 12) & 0xF);
            uint srcNodeId = (uint)(data & 0xFFF);

            return new PakbusHeader(PakbusHeaderType.Normal, dstNodeId, srcNodeId, dstPhyAddr, srcPhyAddr, hiProtoCode, expMoreCode, (PakbusLinkState) linkState,
                                    (PakbusPriority)priority, hopCnt);
        }

        public static PakbusHeader DecompileCompressedLinkState(byte[] data)
        {
            byte linkState = (byte) (data[0] >> 4);
            uint destAddr = ((data[0] & 0xFu) << 8) | data[1];
            byte expMoreCode = (byte) (data[2] >> 6);
            byte priority = (byte) ((data[2] >> 4) & 0x3);
            uint sourceAddr = ((data[2] & 0xFu) << 8) | data[3];

            return new PakbusHeader(PakbusHeaderType.CompressedLinkState, 0, 0, destAddr, sourceAddr, PakbusProtocol.LinkState, expMoreCode, (PakbusLinkState) linkState, (PakbusPriority)priority, 0);
        }

        public static PakbusHeader Create(
            uint from,
            uint to,
            PakbusProtocol protocol,
            PakbusPriority priority = PakbusPriority.High) =>
            new PakbusHeader(PakbusHeaderType.Normal, to, from, to, from,
                             protocol,
                             1, PakbusLinkState.Ready, priority, 0);

        public static PakbusHeader Decompile(byte[] data)
        {
            switch (data.Length)
            {
                case 4:
                    // Compressed link state header
                    return DecompileCompressedLinkState(data);
                case 6:
                    // Uncompressed link state header
                    throw new NotImplementedException();
                case 8:
                    // Normal header
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(data);
                    ulong header_ui64 = BitConverter.ToUInt64(data);
                    return Decompile(header_ui64);
                default:
                    Log.Error("Failed to decipher Pakbus header");
                    return null;
            }
        }
    }
}
