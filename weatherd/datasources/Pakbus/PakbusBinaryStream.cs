using System;
using System.Text;
using weatherd.io;

namespace weatherd.datasources.pakbus
{
    /// <summary>
    ///     Provides helper methods specifically for Pakbus data streams.
    /// </summary>
    public class PakbusBinaryStream : BinaryStream
    {
        /// <inheritdoc />
        public PakbusBinaryStream(byte[] data, Endianness endianness)
            : base(data, endianness)
        {
        }

        /// <inheritdoc />
        public PakbusBinaryStream(Endianness endianness)
            : base(endianness)
        {
        }

        public object Read(Field field)
        {
            return field.Type switch
            {
                PakbusDatumType.Bool8 => ReadByte(),
                PakbusDatumType.Bool2 => ReadByte(),
                PakbusDatumType.Bool4 => ReadByte(),
                PakbusDatumType.Byte => ReadByte(),
                PakbusDatumType.UInt2 => ReadUInt16(),
                PakbusDatumType.Uint4 => ReadUInt32(),
                PakbusDatumType.Int1 => ReadSByte(),
                PakbusDatumType.Int2 => ReadInt16(),
                PakbusDatumType.Int4 => ReadInt32(),
                PakbusDatumType.FP2 => throw new NotImplementedException(),
                PakbusDatumType.FP3 => throw new NotImplementedException(),
                PakbusDatumType.FP4 => ReadFP4(),
                PakbusDatumType.IEEE4B => ReadSingle(Endianness.Big),
                PakbusDatumType.IEEE8B => ReadDouble(Endianness.Big),
                PakbusDatumType.Bool => ReadByte() > 0,
                PakbusDatumType.Sec => throw new NotImplementedException(),
                PakbusDatumType.USec => ReadUSec(),
                PakbusDatumType.NSec => Read<NSec>(),
                PakbusDatumType.ASCII => throw new NotImplementedException(),
                PakbusDatumType.ASCIIZ => ReadStringZ(),
                PakbusDatumType.Short => ReadInt16(),
                PakbusDatumType.Long => ReadInt32(),
                PakbusDatumType.UShort => ReadUInt16(),
                PakbusDatumType.ULong => ReadUInt64(),
                PakbusDatumType.IEEE4L => ReadSingle(Endianness.Little),
                PakbusDatumType.IEEE8L => ReadSingle(Endianness.Little),
                PakbusDatumType.SecNano => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        ///     Reads an FP4 data type from the stream.
        /// </summary>
        /// <returns>A single read from the stream.</returns>
        /// <remarks>
        ///     I'll admit, this one tripped me up at first.
        ///     Pakbus uses an almost-IEEE754 standard single for FP4
        ///     data types, but it differs in that the exponent is 7 bits
        ///     long and the mantissa is 24 bits long, as opposed to the
        ///     IEEE754 standard of an 8 bit exponent and 23 bit mantissa.
        ///     I presume Campbell Scientific did this to increase the
        ///     precision of the FP4 type at the expense of range.
        ///     TODO in the future is to add FP2 and FP3 data types.
        /// </remarks>
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

        /// <summary>
        ///     Reads a string from the stream until a null terminator (0x0) is reached.
        /// </summary>
        /// <returns>The string read to from the stream.</returns>
        public string ReadStringZ()
        {
            StringBuilder sb = new();
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
            int ns = (int)(tmpBuffer % 100000 * 1000);

            return new NSec(ss, ns);
        }

        public void WriteUSec(NSec time)
        {
            ulong ss = (ulong)time.Seconds * 100000ul;
            ulong ns = (ulong)time.Nanoseconds / 1000ul;
            ulong buf = ss + ns;

            buf &= 0xFFFFFFFFFFFF;

            _buffer[0] = (byte)buf;
            _buffer[1] = (byte)(buf >> 8);
            _buffer[2] = (byte)(buf >> 16);
            _buffer[3] = (byte)(buf >> 24);
            _buffer[4] = (byte)(buf >> 32);
            _buffer[5] = (byte)(buf >> 40);
            Write(_buffer, 0, 6);
        }
    }
}
