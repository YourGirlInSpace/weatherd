using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace weatherd.io
{
    public class BigEndianBinaryReader : BinaryReader
    {
        /// <inheritdoc />
        public BigEndianBinaryReader(Stream input) : base(input)
        { }

        /// <inheritdoc />
        public BigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        { }

        /// <inheritdoc />
        public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        { }

        /// <inheritdoc />
        public override ushort ReadUInt16()
        {
            var data = ReadBytes(2, true);
            return BitConverter.ToUInt16(data);
        }

        /// <inheritdoc />
        public override uint ReadUInt32()
        {
            var data = ReadBytes(4, true);
            return BitConverter.ToUInt32(data);
        }

        /// <inheritdoc />
        public override ulong ReadUInt64()
        {
            var data = ReadBytes(8, true);
            return BitConverter.ToUInt64(data);
        }

        /// <inheritdoc />
        public ulong ReadUInt64L()
        {
            return base.ReadUInt64();
        }

        /// <inheritdoc />
        public override short ReadInt16()
        {
            var data = ReadBytes(2, true);
            return BitConverter.ToInt16(data);
        }

        /// <inheritdoc />
        public override int ReadInt32()
        {
            var data = ReadBytes(4, true);
            return BitConverter.ToInt32(data);
        }

        /// <inheritdoc />
        public override long ReadInt64()
        {
            var data = ReadBytes(8, true);
            return BitConverter.ToInt64(data);
        }

        /// <inheritdoc />
        public override float ReadSingle()
        {
            var data = ReadBytes(4, true);
            return BitConverter.ToSingle(data);
        }

        public float ReadHalf()
        {
            ushort u = ReadUInt16();

            int s = u >> 15 == 0 ? -1 : 1;
            int factor = (u & 0x6000) >> 13;
            float abs_val = (float) Math.Pow(10.0, -1 * factor) * (u & 0x1FFF);

            if (abs_val > 6999.0)
                return -9999;

            return s * abs_val;
        }

        public T Read<T>() where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] buf = ReadBytes(size, true);
            GCHandle hPin = GCHandle.Alloc(buf, GCHandleType.Pinned);
            return Marshal.PtrToStructure<T>(hPin.AddrOfPinnedObject());
        }

        public uint ReadUInt32L() => base.ReadUInt32();

        public byte[] ReadBytes(int count, bool bigEndian)
        {
            byte[] buf = base.ReadBytes(count);
            if (BitConverter.IsLittleEndian && bigEndian)
                Array.Reverse(buf);

            return buf;
        }
    }
}
