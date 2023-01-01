using System.IO.Ports;

namespace weatherd.io
{
    /// <summary>
    /// Serial interface for testing purposes.
    /// </summary>
    public interface ISerialInterface
    {
        bool RtsEnable { get; set; }
        bool DtrEnable { get; set; }
        Handshake Handshake { get; set; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }
        bool IsOpen { get; }
        void Open();
        void Write(byte[] data);
        void Write(byte[] data, int index, int length);
        int ReadByte();
        void Flush();
    }
}
