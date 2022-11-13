using System;
using System.IO.Ports;

namespace weatherd.io
{
    internal class SerialInterface
        : ISerialInterface
    {
        private readonly string _portName;
        private readonly int _baud;
        private readonly Parity _parity;
        private readonly int _dataBits;
        private readonly StopBits _stopBits;
        private SerialPort _port;

        public bool RtsEnable { get; set; }
        public bool DtrEnable { get; set; }

        public Handshake Handshake { get; set; }

        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }

        public bool IsOpen => _port.IsOpen;

        public SerialInterface(string portName, int baud, Parity parity, int dataBits, StopBits stopBits)
        {
            if (string.IsNullOrEmpty(portName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(portName));
            if (baud <= 0)
                throw new ArgumentOutOfRangeException(nameof(baud));
            if (dataBits <= 0)
                throw new ArgumentOutOfRangeException(nameof(dataBits));
            _portName = portName;
            _baud = baud;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
        }

        public void Open()
        {
            _port = new SerialPort(_portName, _baud, _parity, _dataBits, _stopBits)
            {
                RtsEnable = RtsEnable,
                DtrEnable = DtrEnable,

                ReadTimeout = ReadTimeout,
                WriteTimeout = WriteTimeout,

                Handshake = Handshake
            };

            _port.Open();
        }

        public void Write(byte[] data)
            => Write(data, 0, data.Length);

        public void Write(byte[] data, int index, int length)
            => _port.Write(data, index, length);

        public int ReadByte() => _port.ReadByte();

        public void Flush() => _port.BaseStream.Flush();
    }
}
