using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace weatherd.io
{
    public enum Endianness
    {
        Big,
        Little
    }

    /// <summary>
    ///     Provides an interface to read or write from a stream
    ///     with support for various byte orders.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    public class BinaryStream : IDisposable
    {
        private const int MinimumBufferSize = 16;

        public Stream BaseStream { get; }

        public BinaryStream()
            : this(new MemoryStream(), Encoding.ASCII, GetSystemEndianness())
        {
        }

        public BinaryStream(Endianness defaultEndianness)
            : this(new MemoryStream(), Encoding.ASCII, defaultEndianness)
        {
        }

        public BinaryStream(byte[] buffer)
            : this(buffer, Encoding.ASCII, GetSystemEndianness())
        {
        }

        public BinaryStream(byte[] buffer, Endianness defaultEndianness)
            : this(buffer, Encoding.ASCII, defaultEndianness)
        {
        }

        public BinaryStream(byte[] buffer, Encoding encoding)
            : this(buffer, encoding, GetSystemEndianness())
        {
        }

        public BinaryStream(byte[] buffer, Encoding encoding, Endianness defaultEndianness)
            : this(new MemoryStream(buffer, true), encoding, defaultEndianness)
        {
        }

        public BinaryStream(Stream stream)
            : this(stream, Encoding.ASCII, GetSystemEndianness())
        {
        }

        public BinaryStream(Stream stream, Endianness defaultEndianness)
            : this(stream, Encoding.ASCII, defaultEndianness)
        {
        }

        public BinaryStream(Stream stream, Encoding encoding)
            : this(stream, encoding, GetSystemEndianness())
        {
        }

        public BinaryStream(Stream stream, Encoding encoding, Endianness defaultEndianness)
        {
            BaseStream = stream;
            _buffer = new byte[MinimumBufferSize];

            _decoder = encoding.GetDecoder();
            _2ByteChar = encoding is UnicodeEncoding;
            _defaultEndianness = defaultEndianness;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        private static Endianness GetSystemEndianness() =>
            BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big;

        public virtual void Skip(long bytes) => BaseStream.Seek(bytes, SeekOrigin.Current);

        public virtual void Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

        protected static bool MustReverse(Endianness endianness) =>
            (endianness == Endianness.Little && !BitConverter.IsLittleEndian) ||
            (endianness == Endianness.Big && BitConverter.IsLittleEndian);

        public byte[] ToArray()
        {
            Seek(0, SeekOrigin.Begin);

            if (BaseStream is MemoryStream ms)
                return ms.ToArray();

            byte[] b = new byte[BaseStream.Length];
            for (int i = 0; i < b.Length; i++)
                b[i] = (byte)BaseStream.ReadByte();

            return b;
        }

        protected T Read<T>() where T : struct
            => Read<T>(_defaultEndianness);

        public T Read<T>(Endianness endianness) where T : struct
        {
            int len = Marshal.SizeOf<T>();

            byte[] sBuf = new byte[len];
            int bytesRead = BaseStream.Read(sBuf, 0, sBuf.Length);
            if (bytesRead != len)
                throw new EndOfStreamException();

            if (MustReverse(endianness))
                Array.Reverse(sBuf);

            GCHandle handle = GCHandle.Alloc(sBuf, GCHandleType.Pinned);
            IntPtr ptr = handle.AddrOfPinnedObject();

            return Marshal.PtrToStructure<T>(ptr);
        }

        protected readonly bool _2ByteChar;
        protected readonly byte[] _buffer;

        protected readonly Decoder _decoder;
        protected readonly Endianness _defaultEndianness;

        #region Reading Operations

        protected void FillBuffer(int count, bool reverse = false)
        {
            int bytesRead = BaseStream.Read(_buffer, 0, count);
            if (bytesRead < count)
                throw new EndOfStreamException();

            if (reverse)
                Array.Reverse(_buffer, 0, bytesRead);
        }

        public virtual bool ReadBoolean()
        {
            FillBuffer(1);

            return _buffer[0] != 0;
        }

        public virtual byte ReadByte()
        {
            if (BaseStream is null)
                throw new InvalidOperationException("Stream is null");

            FillBuffer(1);
            return _buffer[0];
        }

        public sbyte ReadSByte()
        {
            FillBuffer(1);
            return (sbyte)_buffer[0];
        }

        public virtual char ReadChar()
        {
            if (_2ByteChar)
                throw new NotImplementedException("Unicode encoding is not supported");

            int value = BaseStream.ReadByte();
            if (value == -1)
                throw new EndOfStreamException();

            return (char)value;
        }

        public virtual short ReadInt16()
            => ReadInt16(_defaultEndianness);

        public virtual short ReadInt16(Endianness endianness)
        {
            bool reverse = MustReverse(endianness);
            FillBuffer(2, reverse);

            return (short)(_buffer[0] | (_buffer[1] << 8));
        }

        public virtual ushort ReadUInt16()
            => ReadUInt16(_defaultEndianness);

        public virtual ushort ReadUInt16(Endianness endianness)
        {
            bool reverse = MustReverse(endianness);
            FillBuffer(2, reverse);

            return (ushort)(_buffer[0] | (_buffer[1] << 8));
        }

        public virtual int ReadInt32()
            => ReadInt32(_defaultEndianness);

        public virtual int ReadInt32(Endianness endianness)
        {
            bool reverse = MustReverse(endianness);
            FillBuffer(4, reverse);

            return _buffer[0]
                   | (_buffer[1] << 8)
                   | (_buffer[2] << 16)
                   | (_buffer[3] << 24);
        }

        public virtual uint ReadUInt32()
            => ReadUInt32(_defaultEndianness);

        public virtual uint ReadUInt32(Endianness endianness)
        {
            bool reverse = MustReverse(endianness);
            FillBuffer(4, reverse);

            return (uint)(_buffer[0]
                          | (_buffer[1] << 8)
                          | (_buffer[2] << 16)
                          | (_buffer[3] << 24));
        }

        public virtual long ReadInt64()
            => ReadInt64(_defaultEndianness);

        public virtual long ReadInt64(Endianness endianness)
        {
            bool reverse = MustReverse(endianness);
            FillBuffer(4, reverse);

            uint lo = (uint)(_buffer[0]
                             | (_buffer[1] << 8)
                             | (_buffer[2] << 16)
                             | (_buffer[3] << 24));
            uint hi = (uint)(_buffer[4]
                             | (_buffer[5] << 8)
                             | (_buffer[6] << 16)
                             | (_buffer[7] << 24));

            return (long)(((ulong)hi << 32) | lo);
        }

        public virtual ulong ReadUInt64()
            => ReadUInt64(_defaultEndianness);

        public virtual ulong ReadUInt64(Endianness endianness)
        {
            bool reverse = MustReverse(endianness);
            FillBuffer(4, reverse);

            uint lo = (uint)(_buffer[0]
                             | (_buffer[1] << 8)
                             | (_buffer[2] << 16)
                             | (_buffer[3] << 24));
            uint hi = (uint)(_buffer[4]
                             | (_buffer[5] << 8)
                             | (_buffer[6] << 16)
                             | (_buffer[7] << 24));

            return ((ulong)hi << 32) | lo;
        }

        [SecuritySafeCritical]
        public virtual float ReadSingle()
            => ReadSingle(_defaultEndianness);

        [SecuritySafeCritical]
        public virtual unsafe float ReadSingle(Endianness endianness)
        {
            FillBuffer(4, MustReverse(endianness));

            uint tmpBuffer = (uint)(_buffer[0]
                                    | (_buffer[1] << 8)
                                    | (_buffer[2] << 16)
                                    | (_buffer[3] << 24));
            return *(float*)&tmpBuffer;
        }

        [SecuritySafeCritical]
        public virtual double ReadDouble()
            => ReadDouble(_defaultEndianness);

        [SecuritySafeCritical]
        public virtual unsafe double ReadDouble(Endianness endianness)
        {
            FillBuffer(8, MustReverse(endianness));
            uint lo = (uint)(_buffer[0]
                             | (_buffer[1] << 8)
                             | (_buffer[2] << 16)
                             | (_buffer[3] << 24));
            uint hi = (uint)(_buffer[4]
                             | (_buffer[5] << 8)
                             | (_buffer[6] << 16)
                             | (_buffer[7] << 24));

            ulong tmpBuffer = ((ulong)hi << 32) | lo;
            return *(double*)&tmpBuffer;
        }

        public virtual string ReadString(int length)
        {
            int n = 0;
            long opos = 0;

            if (BaseStream.CanSeek)
                opos = BaseStream.Position;

            byte[] b_buffer = new byte[length];
            while (n < length)
            {
                try
                {
                    b_buffer[n] = ReadByte();
                } catch
                {
                    if (BaseStream.CanSeek)
                        BaseStream.Seek(opos - BaseStream.Position, SeekOrigin.Current);
                    throw;
                }

                n++;
            }

            char[] c_buffer = new char[b_buffer.Length];
            _decoder.GetChars(b_buffer, 0, b_buffer.Length, c_buffer, 0, true);

            return new string(c_buffer);
        }

        #endregion

        #region Writing Operations

        [SecuritySafeCritical] // auto-generated
        public virtual void Write(double value)
            => Write(value, _defaultEndianness);

        // Writes a two-byte signed integer to this stream. The current position of
        // the stream is advanced by two.
        // 
        public virtual void Write(short value)
            => Write(value, _defaultEndianness);

        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        // 
        public virtual void Write(ushort value)
            => Write(value, _defaultEndianness);

        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public virtual void Write(int value)
            => Write(value, _defaultEndianness);

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public virtual void Write(uint value)
            => Write(value, _defaultEndianness);

        // Writes an eight-byte signed integer to this stream. The current position
        // of the stream is advanced by eight.
        // 
        public virtual void Write(long value)
            => Write(value, _defaultEndianness);

        // Writes an eight-byte unsigned integer to this stream. The current 
        // position of the stream is advanced by eight.
        // 
        public virtual void Write(ulong value)
            => Write(value, _defaultEndianness);

        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        // 
        [SecuritySafeCritical] // auto-generated
        public virtual void Write(float value)
            => Write(value, _defaultEndianness);

        public virtual void Write(byte value)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _buffer[0] = value;
            Write(_buffer, 0, 1);
        }

        public virtual void Write(sbyte value)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _buffer[0] = (byte)value;
            Write(_buffer, 0, 1);
        }

        public virtual void Write(byte[] buffer, int start, int count)
            => Write(buffer, start, count, _defaultEndianness);

        public virtual void Write(byte[] buffer, int start, int count, Endianness endianness)
        {
            if (MustReverse(endianness))
                Array.Reverse(buffer, start, count);

            BaseStream.Write(buffer, start, count);
        }

        [SecuritySafeCritical] // auto-generated
        public virtual unsafe void Write(double value, Endianness endianness)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");

            ulong TmpValue = *(ulong*)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            _buffer[4] = (byte)(TmpValue >> 32);
            _buffer[5] = (byte)(TmpValue >> 40);
            _buffer[6] = (byte)(TmpValue >> 48);
            _buffer[7] = (byte)(TmpValue >> 56);
            Write(_buffer, 0, 8);
        }

        // Writes a two-byte signed integer to this stream. The current position of
        // the stream is advanced by two.
        // 
        public virtual void Write(short value, Endianness endianness)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");

            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            Write(_buffer, 0, 2);
        }

        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        // 
        public virtual void Write(ushort value, Endianness endianness)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");

            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            Write(_buffer, 0, 2);
        }

        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public virtual void Write(int value, Endianness endianness)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");

            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            Write(_buffer, 0, 4);
        }

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public virtual void Write(uint value, Endianness endianness)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");

            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            Write(_buffer, 0, 4);
        }

        // Writes an eight-byte signed integer to this stream. The current position
        // of the stream is advanced by eight.
        // 
        public virtual void Write(long value, Endianness endianness)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");

            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);
            Write(_buffer, 0, 8);
        }

        // Writes an eight-byte unsigned integer to this stream. The current 
        // position of the stream is advanced by eight.
        // 
        public virtual void Write(ulong value, Endianness endianness)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");

            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);
            Write(_buffer, 0, 8);
        }

        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        // 
        [SecuritySafeCritical] // auto-generated
        public virtual unsafe void Write(float value, Endianness endianness)
        {
            if (!BaseStream.CanWrite)
                throw new InvalidOperationException("Cannot write to this stream");

            uint TmpValue = *(uint*)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            Write(_buffer, 0, 4);
        }

        #endregion
    }
}
