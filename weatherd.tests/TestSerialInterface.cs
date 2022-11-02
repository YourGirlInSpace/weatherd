using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using weatherd.datasources.pakbus;
using weatherd.io;

namespace weatherd.tests
{
    public class TestSerialInterface : ISerialInterface
    {
        /// <inheritdoc />
        public bool RtsEnable { get; set; }

        /// <inheritdoc />
        public bool DtrEnable { get; set; }

        /// <inheritdoc />
        public Handshake Handshake { get; set; }

        /// <inheritdoc />
        public int ReadTimeout { get; set; }

        /// <inheritdoc />
        public int WriteTimeout { get; set; }

        /// <inheritdoc />
        public bool IsOpen { get; private set; } = true;

        // This is the bytes being sent from the device
        private readonly Queue<byte> _deviceQueue;
        // This is the bytes being sent from the host
        private readonly Queue<byte> _hostQueue;

        public int DeviceBytesToRead => _deviceQueue.Count;
        public int HostBytesToRead => _hostQueue.Count;

        public TestSerialInterface()
        {
            _deviceQueue = new Queue<byte>();
            _hostQueue = new Queue<byte>();
        }

        /// <inheritdoc />
        public void Open()
        {
            IsOpen = true;
        }

        /// <inheritdoc />
        public void Write(byte[] data)
            => Write(data, 0, data.Length);

        /// <inheritdoc />
        public void Write(byte[] data, int index, int length)
        {
            for (int i = 0; i < length; i++)
                _hostQueue.Enqueue(data[index + i]);
        }

        /// <inheritdoc />
        public int ReadByte() => _deviceQueue.Dequeue();

        /// <inheritdoc />
        public void Flush()
        {
            if (_hostQueue.Count < 8)
                return;

            byte[] buffer = new byte[PakbusPacket.MaxLength];
            for (int i = 0; i < _hostQueue.Count; i++)
            {
                //_hostQueue.Enqueue(data[index + i]);
                byte b = _hostQueue.Dequeue();

                if (b != 0xBD)
                    continue;

                int lastByte;
                while ((lastByte = _hostQueue.Dequeue()) == 0xBD)
                {
                    // ignore
                }

                int n = 0;
                buffer[n++] = 0xBD;
                buffer[n++] = (byte)lastByte;
                while (n < buffer.Length)
                {
                    b = _hostQueue.Dequeue();
                    buffer[n++] = b;

                    if (b == 0xBD)
                        break;
                }

                if (n == buffer.Length)
                    continue; // malformed

                try
                {
                    IEnumerable<byte> resp = CR10XSimulator.HandlePacket(buffer[..n]);

                    foreach (byte rB in resp)
                        _deviceQueue.Enqueue(rB);
                } catch (Exception ex)
                {

                }
            }
        }

        #region Testing Methods
        internal void WriteDevice(byte[] data)
        {
            foreach (byte b in data)
                _deviceQueue.Enqueue(b);
        }
        
        internal void WriteDevice(byte[] data, int index, int length)
        {
            for (int i = 0; i < length; i++)
                _deviceQueue.Enqueue(data[index + i]);
        }
        
        internal int ReadHostByte() => _hostQueue.Dequeue();
        #endregion
    }
}
