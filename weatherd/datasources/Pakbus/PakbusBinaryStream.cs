using System;
using System.Text;
using Serilog;
using weatherd.io;

namespace weatherd.datasources.Pakbus
{
    public class PakbusBinaryStream : BinaryStream
    {
        public PakbusBinaryStream(byte[] data, Endianness endianness)
            : base(data, endianness)
        { }

        public PakbusBinaryStream(Endianness endianness)
            : base(endianness)
        { }

        public object Read(Field field)
        {
            switch (field.Type)
            {
                case PakbusDatumType.Bool8:
                case PakbusDatumType.Bool2:
                case PakbusDatumType.Bool4:
                case PakbusDatumType.Byte:
                    return ReadByte();
                case PakbusDatumType.UInt2:
                    return ReadUInt16();
                case PakbusDatumType.Uint4:
                    return ReadUInt32();
                case PakbusDatumType.Int1:
                    return ReadSByte();
                case PakbusDatumType.Int2:
                    return ReadInt16();
                case PakbusDatumType.Int4:
                    return ReadInt32();
                case PakbusDatumType.FP2:
                case PakbusDatumType.FP3:
                    throw new NotImplementedException();
                case PakbusDatumType.FP4:
                    return ReadFP4();
                case PakbusDatumType.IEEE4B:
                    return ReadSingle(Endianness.Big);
                case PakbusDatumType.IEEE8B:
                    return ReadDouble(Endianness.Big);
                case PakbusDatumType.Bool:
                    return ReadByte() > 0;
                case PakbusDatumType.Sec:
                    throw new NotImplementedException();
                case PakbusDatumType.USec:
                    return ReadUSec();
                case PakbusDatumType.NSec:
                    return Read<NSec>();
                case PakbusDatumType.ASCII:
                    throw new NotImplementedException();
                case PakbusDatumType.ASCIIZ:
                    return ReadStringZ();
                case PakbusDatumType.Short:
                    return ReadInt16();
                case PakbusDatumType.Long:
                    return ReadInt32();
                case PakbusDatumType.UShort:
                    return ReadUInt16();
                case PakbusDatumType.ULong:
                    return ReadUInt64();
                case PakbusDatumType.IEEE4L:
                    return ReadSingle(Endianness.Little);
                case PakbusDatumType.IEEE8L:
                    return ReadSingle(Endianness.Little);
                case PakbusDatumType.SecNano:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
        
        public float ReadFP4()
        {
            uint bits = ReadUInt32();
            
            int sign = bits >> 31 == 0 ? 1 : -1;
            int expSign = (bits & 0x40000000) == 0 ? -1 : 1;
            int exponent = (sbyte)((bits & 0x3F000000) >> 24) * expSign;
            uint mantissa = bits & 0xFFFFFF;
            float fractional = mantissa / 16777216f;
            
            float result = (float)(sign * fractional * Math.Pow(2, exponent));

            if (Math.Abs(result) < 1e-10)
                return 0;

            return result;
        }

        public string ReadStringZ()
        {
            StringBuilder sb = new StringBuilder();
            while (BaseStream.Position < BaseStream.Length)
            {
                char c = ReadChar();
                if (c == 0x0)
                    break;

                sb.Append(c);
            }

            return sb.ToString();
        }


        public NSec ReadUSec()
            => ReadUSec(_defaultEndianness);

        public NSec ReadUSec(Endianness endianness)
        {
            FillBuffer(6, MustReverse(endianness));
            
            ulong tmpBuffer = _buffer[0]
                              | ((ulong)_buffer[1] << 8)
                              | ((ulong)_buffer[2] << 16)
                              | ((ulong)_buffer[3] << 24)
                              | ((ulong)_buffer[4] << 32)
                              | ((ulong)_buffer[5] << 40);

            int ss = (int)(tmpBuffer / 100000);
            int ns = (int)((tmpBuffer % 100000) * 1000);

            return new NSec(ss, ns);
        }

        public void WriteUSec(NSec time)
        {
            ulong ss = (ulong) time.Seconds * 100000ul;
            ulong ns = (ulong) time.Nanoseconds / 1000ul;
            ulong buf = ss + ns;

            buf &= 0xFFFFFFFFFFFF;

            _buffer[0] = (byte) buf;
            _buffer[1] = (byte) (buf >> 8);
            _buffer[2] = (byte) (buf >> 16);
            _buffer[3] = (byte) (buf >> 24);
            _buffer[4] = (byte) (buf >> 32);
            _buffer[5] = (byte) (buf >> 40);
            Write(_buffer, 0, 6);
        }
    }
}
